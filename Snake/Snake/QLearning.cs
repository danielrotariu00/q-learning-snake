using System;
using System.Collections.Generic;

namespace Snake
{
    public class QLearning
    {
        private Dictionary<String, double[]> qTable = new Dictionary<String, double[]>();

        private readonly Random random = new Random();
        private double epsilon = 0.1;
        private double learningRate = 0.1;
        private double discountFactor = 0.1;

        private int GAME_OVER_REWARD = -1000;
        private int EAT_FOOD_REWARD = 10;
        private int MOVE_CLOSER_TO_FOOD_REWARD = 1;
        private int MOVE_AWAY_FROM_FOOD_REWARD = -1;

        public QLearning()
        {
            InitializeQTable();
        }

        public QLearning(double epsilon, double learningRate, double discountFactor)
        {
            InitializeQTable();

            this.epsilon = epsilon;
            this.learningRate = learningRate;
            this.discountFactor = discountFactor;
        }

        public Direction GetNextMove(GameState gameState, bool isTraining)
        {
            int action;
            double prob = random.NextDouble();
            String currentState = GetState(gameState);

            if (isTraining && prob <= epsilon)
            {
                action = GetRandomAction();
            } 
            else
            {
                action = GetBestAction(currentState);
            }

            Direction direction = GetActionDirection(gameState, action);
            GameState nextGameState = gameState.DeepClone();

            nextGameState.ChangeDirection(direction);
            nextGameState.Move();

            if (isTraining)
            {
                UpdateQTable(gameState, action, nextGameState);
            }

            return direction;
        }

        private String GetState(GameState gameState)
        {
            Direction headDirection = gameState.Dir;
            Direction foodDirection = gameState.Dir;
            bool dangerLeft;
            bool dangerRight;
            bool dangerUp;
            bool dangerDown;

            Position headPosition = gameState.HeadPosition();
            Position foodPosition = gameState.foodPosition;


            if (headPosition.Col > foodPosition.Col)
            {
                foodDirection = Direction.Left;

                if (headPosition.Row > foodPosition.Row)
                {
                    foodDirection = Direction.LeftUp;
                }
                else if (headPosition.Row < foodPosition.Row)
                {
                    foodDirection = Direction.LeftDown;
                }
            }
            else if (headPosition.Col < foodPosition.Col)
            {
                foodDirection = Direction.Right;

                if (headPosition.Row > foodPosition.Row)
                {
                    foodDirection = Direction.RightUp;
                }
                else if (headPosition.Row < foodPosition.Row)
                {
                    foodDirection = Direction.RightDown;
                }
            } 
            else
            {
                if (headPosition.Row > foodPosition.Row)
                {
                    foodDirection = Direction.Up;
                }
                else if (headPosition.Row < foodPosition.Row)
                {
                    foodDirection = Direction.Down;
                }
            }

            dangerLeft = IsDanger(gameState, headPosition, Direction.Left);
            dangerRight = IsDanger(gameState, headPosition, Direction.Right);
            dangerUp = IsDanger(gameState, headPosition, Direction.Up);
            dangerDown = IsDanger(gameState, headPosition, Direction.Down);

            return $"{headDirection.GetHashCode()},{foodDirection.GetHashCode()},{dangerLeft},{dangerRight},{dangerUp},{dangerDown}";
        }

        private int GetRandomAction()
        {
            return random.Next(0, 3);
        }

        private int GetBestAction(String state)
        {
            double[] actionsValues = qTable.GetValueOrDefault(state);

            int bestAction = 0;
            for(int i = 1; i < actionsValues.Length; i++)
            {
                if (actionsValues[i] > actionsValues[bestAction])
                {
                    bestAction = i;
                }
            }

            return bestAction;
        }

        private Direction GetActionDirection(GameState gameState, int action)
        {
            Direction headDirection = gameState.Dir;

            switch (action)
            {
                case 1:
                    if (headDirection == Direction.Up || headDirection == Direction.Down)
                    {
                        return Direction.Left;
                    }
                    else
                    {
                        return Direction.Up;
                    }
                case 2:
                    if (headDirection == Direction.Up || headDirection == Direction.Down)
                    {
                        return Direction.Right;
                    }
                    else
                    {
                        return Direction.Down;
                    }
                default:
                    return headDirection;
            }
        }

        private bool IsDanger(GameState gameState, Position headPosition, Direction direction)
        {
            Position newHeadPos = headPosition.Translate(direction);
            GridValue hit = gameState.WillHit(newHeadPos);

            if (hit == GridValue.Outside || hit == GridValue.Snake)
            {
                return true;
            }

            return false;
        }

        private void UpdateQTable(GameState gameState, int action, GameState nextGameState)
        {
            String oldState = GetState(gameState);
            String newState = GetState(nextGameState);
            int reward;

            if (nextGameState.GameOver)
            {
                reward = GAME_OVER_REWARD;

                // Bellman Equation - No Future State
                qTable[oldState][action] = qTable[oldState][action] + learningRate * (reward - qTable[oldState][action]);
            }
            else
            {
                if (nextGameState.HeadPosition().Equals(gameState.foodPosition))
                {
                    reward = EAT_FOOD_REWARD;
                }
                else if (nextGameState.RowDistanceFromFood() < gameState.RowDistanceFromFood() ||
                    nextGameState.ColDistanceFromFood() < gameState.ColDistanceFromFood())
                {
                    reward = MOVE_CLOSER_TO_FOOD_REWARD;
                }
                else
                {
                    reward = MOVE_AWAY_FROM_FOOD_REWARD;
                }

                // Bellman Equation
                int nextStateBestAction = GetBestAction(newState);
                qTable[oldState][action] = qTable[oldState][action] + learningRate * 
                    (reward + discountFactor * qTable[newState][nextStateBestAction] - qTable[oldState][action]);
            }
        }

        private void InitializeQTable()
        {
            // State = (Snake head direction, Food direction relative to snake head, DangerLeft, DangerRight, DangerUp, DangerDown)
            // Action = [No action, Valid action 1, Valid action 2]
            //      snake head direction = left => valid actions are up and down
            //      snake head direction = right => valid actions are up and down
            //      snake head direction = up => valid actions are left and right
            //      snake head direction = down => valid actions are left and right

            Direction[] headDirections = new Direction[4]
            {
                Direction.Left,
                Direction.Right,
                Direction.Up,
                Direction.Down,
            };

            Direction[] foodDirections = new Direction[8]
            {
                Direction.Left,
                Direction.Right,
                Direction.Up,
                Direction.Down,
                Direction.LeftUp,
                Direction.LeftDown,
                Direction.RightUp,
                Direction.RightDown
            };

            bool[] dangerLeftOptions = new bool[2]
            {
                false,
                true
            };

            bool[] dangerRightOptions = new bool[2]
            {
                false,
                true
            };

            bool[] dangerUpOptions = new bool[2]
            {
                false,
                true
            };

            bool[] dangerDownOptions = new bool[2]
            {
                false,
                true
            };

            foreach (Direction headDirection in headDirections)
            {
                foreach (Direction foodDirection in foodDirections)
                {
                    foreach (bool dangerLeft in dangerLeftOptions)
                    {
                        foreach (bool dangerRight in dangerRightOptions)
                        {
                            foreach (bool dangerUp in dangerUpOptions)
                            {
                                foreach (bool dangerDown in dangerDownOptions)
                                {
                                    String key = $"{headDirection.GetHashCode()},{foodDirection.GetHashCode()},{dangerLeft},{dangerRight},{dangerUp},{dangerDown}";
                                    qTable.Add(key, new double[3] { 0, 0, 0 });
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
