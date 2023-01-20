using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<GridValue, ImageSource> gridValToImage = new()
        {
            { GridValue.Empty, Images.Empty },
            { GridValue.Snake, Images.Body },
            { GridValue.Food, Images.Food }
        };

        private readonly Dictionary<Direction, int> dirToRotation = new()
        {
            { Direction.Up, 0 },
            { Direction.Right, 90 },
            { Direction.Down, 180 },
            { Direction.Left, 270 }
        };

        private Image[,] gridImages;
        private readonly Random random = new Random();
        private GameState gameState;
        private QLearning qLearning = new QLearning();
        private bool isRunning;
        private bool isTraining = false;

        private int rows = 20, cols = 20;
        private int numberOfTrainingGames = 500;
        private int trainingSpeedDelay = 2;
        private int testingSpeedDelay = 50;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void TrainButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                isRunning = true;
                isTraining = true;

                Setup();
                InitializeQLearning();
                InitializeGameState();

                await RunTraining();
                isRunning = false;
                isTraining = false;
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                isRunning = true;

                Setup();
                InitializeGameState();

                await RunTesting();
                isRunning = false;
            }
        }

        private async Task RunTraining()
        {
            int currentGame = 1;

            Draw();
            Overlay.Visibility = Visibility.Hidden;

            while (currentGame <= numberOfTrainingGames)
            {
                GamesCountText.Text = $"TRAINING AGENT - Game: {currentGame}/{numberOfTrainingGames}";
                await TrainingLoop();
                await DrawDeadSnake();
                await Task.Delay(trainingSpeedDelay);
                currentGame++;
                InitializeGameState();
            }

            ShowTrainingOver();
        }

        private async Task TrainingLoop()
        {
            while (!gameState.GameOver)
            {
                await Task.Delay(trainingSpeedDelay);
                MoveAndDraw();
            }
        }

        private async Task RunTesting()
        {
            Draw();
            Overlay.Visibility = Visibility.Hidden;
            await TestingLoop();
            await DrawDeadSnake();
            ShowTestingOver();
        }

        private async Task TestingLoop()
        {
            while (!gameState.GameOver)
            {
                GamesCountText.Text = $"TESTING AGENT - Score: {gameState.Score}";
                await Task.Delay(testingSpeedDelay);
                MoveAndDraw();
            }
        }

        private void MoveAndDraw()
        {
            Direction nextDirection = qLearning.GetNextMove(gameState, isTraining);
            gameState.ChangeDirection(nextDirection);
            int oldScore = gameState.Score;
            gameState.Move();
            if (gameState.Score > oldScore)
            {
                AddFood();
            }
            Draw();
        }

        private Image[,] SetupGrid()
        {
            Image[,] images = new Image[rows, cols];
            GameGrid.Rows = rows;
            GameGrid.Columns = cols;
            GameGrid.Width = GameGrid.Height * (cols / (double)rows);
            GameGrid.Children.Clear();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Image image = new Image()
                    {
                        Source = Images.Empty,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    images[r, c] = image;
                    GameGrid.Children.Add(image);
                }
            }

            return images;
        }

        private void AddFood()
        {
            List<Position> empty = new List<Position>(gameState.EmptyPositions());

            if (empty.Count == 0)
            {
                return;
            }

            Position foodPosition = empty[random.Next(empty.Count)];
            gameState.AddFood(foodPosition);
        }

        private void Draw()
        {
            DrawGrid();
            DrawSnakeHead();
        }

        private void DrawGrid()
        {
            for (int r = 0; r < rows; r ++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GridValue gridVal = gameState.Grid[r, c];
                    gridImages[r, c].Source = gridValToImage[gridVal];
                    gridImages[r, c].RenderTransform = Transform.Identity;
                }
            }
        }

        private void DrawSnakeHead()
        {
            Position headPos = gameState.HeadPosition();
            Image image = gridImages[headPos.Row, headPos.Col];
            image.Source = Images.Head;

            int rotation = dirToRotation[gameState.Dir];
            image.RenderTransform = new RotateTransform(rotation);
        }

        private async Task DrawDeadSnake()
        {
            List<Position> positions = new List<Position>(gameState.SnakePositions());

            for (int i = 0; i < positions.Count; i++)
            {
                Position pos = positions[i];
                ImageSource source = (i == 0) ? Images.DeadHead : Images.DeadBody;
                gridImages[pos.Row, pos.Col].Source = source;

                if (isTraining)
                {
                    await Task.Delay(trainingSpeedDelay);
                }
                else
                {
                    await Task.Delay(testingSpeedDelay);
                }
                
            }
        }

        private void Setup()
        {
            numberOfTrainingGames = Int32.Parse(NumberOfTrainingGamesTextBox.Text);
            rows = Int32.Parse(RowsTextBox.Text);
            cols = Int32.Parse(ColumnsTextBox.Text);
            trainingSpeedDelay = Int32.Parse(TrainingSpeedDelayTextBox.Text);
            testingSpeedDelay = Int32.Parse(TestingSpeedDelayTextBox.Text);

            gridImages = SetupGrid();
        }

        private void InitializeQLearning()
        {
            double epsilon = Double.Parse(EpsilonTextBox.Text);
            double learningRate = Double.Parse(LearningRateTextBox.Text);
            double discountFactor = Double.Parse(DiscountFactorTextBox.Text);

            qLearning = new QLearning(epsilon, learningRate, discountFactor);
        }

        private void InitializeGameState()
        {
            gameState = new GameState(rows, cols);
            AddFood();
        }

        private void ShowTrainingOver()
        {
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "Training done. Press the Test button to test the agent.";
        }

        private void ShowTestingOver()
        {
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "Testing done.";
        }
    }
}
