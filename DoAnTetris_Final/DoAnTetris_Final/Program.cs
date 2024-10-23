using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;
using System.Media;
using System.Threading.Tasks.Dataflow;
using System.Drawing;



namespace Tetris
{
    class Program1
    {
        #region Hướng Dẫn
        static void ShowInstructions()
        {
            Console.Clear();
            DrawBorder();

            string[] instructions = new string[]
            {
        "+----------------Instructions--------------+",
        "| Controls:                                |",
        "| Left/Right Arrow or A/D: Move piece left |",
        "| and right                                |",
        "|                                          |",
        "| Down Arrow or S: Soft drop               |",
        "|                                          |",
        "| Up Arrow or W: Rotate piece              |",
        "|                                          |",
        "| Spacebar: Hard drop                      |",
        "|                                          |",
        "| Enter: Hold piece                        |",
        "|                                          |",
        "| Esc: Pause/Quit game                     |",
        "|                                          |",
        "| Scoring:                                 |",
        "| - Points are awarded for clearing lines  |",
        "| - Level increases every 5 lines cleared  |",
        "| - Game speed increases with each level   |",
        "|                                          |",
        "| Press any key to return to the main menu.|",
        "+------------------------------------------+"
            };

            int windowWidth = 40;
            int windowHeight = 20;
            int startY = Math.Max(0, (windowHeight - instructions.Length) / 2);
            foreach (string line in instructions)
            {
                int startX = Math.Max(0, (windowWidth - line.Length) / 2 + 1);
                Console.SetCursorPosition(startX, startY++);
                Console.WriteLine(line);
            }
            Console.ReadKey(true);
            TitleScreen();
        }

        #endregion


        #region NhạcGame

        static SoundPlayer titlePlayer = new SoundPlayer();
        static SoundPlayer gamePlayer = new SoundPlayer();       //khởi tạo các hàm chạy nhạc game
        static SoundPlayer effectPlayer = new SoundPlayer();

        static void PlaySound(SoundPlayer player, string soundFile, bool loop = false)
        {
            player.SoundLocation = soundFile;
            if (loop)
            {
                player.PlayLooping();
            }
            else
            {
                player.Play();
            }
        }

        static void StopSound(SoundPlayer player)
        {
            player.Stop();
        }

        #endregion


        #region Chế Độ Game

        //---------------------Che Do Game va Enum--------------------//


        static GameMode gameMode = GameMode.Classic;
        static PuzzleDifficulty puzzleDifficulty;
        static int movesRemaining;
        static int targetLines;


        enum GameMode         // Các chế độ game
        {
            Classic,
            Puzzle,
            ClassicWithBlocks
        }

        enum PuzzleDifficulty  // Độ khó cho game mode Puzzle 
        {
            Easy,
            Medium,
            Hard
        }

        enum ClWBDifficulty   // Độ khó cho game mode Classic With Blocks
        {
            Easy,
            Medium,
            Hard
        }


        //-------------------------------Che Do Game va Enum-------------------------------------------//

        #endregion


        #region Assets


       
        readonly static ConsoleColor[] colours =
        {
            ConsoleColor.Red,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Magenta,                    //lưu trữ giá trị màu cho các khối
            ConsoleColor.Yellow,
            ConsoleColor.White,
            ConsoleColor.Cyan
        };

        static ConsoleColor[] blockColours = new ConsoleColor[7];


        readonly static string characters = "#######";
        readonly static int[,,,] positions =
        {
        {
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}}
        },

        {
        {{2,0},{2,1},{2,2},{2,3}},
        {{0,2},{1,2},{2,2},{3,2}},
        {{1,0},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{3,1}},
        },
        {
        {{1,0},{1,1},{1,2},{2,2}},
        {{1,2},{1,1},{2,1},{3,1}},
        {{1,1},{2,1},{2,2},{2,3}},
        {{2,1},{2,2},{1,2},{0,2}}
        },

        {
        {{2,0},{2,1},{2,2},{1,2}},
        {{1,1},{1,2},{2,2},{3,2}},
        {{2,1},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{2,2}}
        },

        {
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}},
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}}
        },
        {
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}},
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}}
        },

        {
        {{0,1},{1,1},{1,0},{2,1}},
        {{1,0},{1,1},{2,1},{1,2}},
        {{0,1},{1,1},{1,2},{2,1}},
        {{1,0},{1,1},{0,1},{1,2}}
        }
        };
        #endregion


        #region BiếnTròChơi
        //---------------------------------------hằng số và các biến của trò chơi----------------------------------------//


        // Map / BG 
        const int mapSizeX = 10;
        const int mapSizeY = 20;
        static char[,] bg = new char[mapSizeY, mapSizeX];

        static int highScore = 0; // Biến lưu điểm cao nhất
        static string highScoreFile = "highscore.txt"; // Tên tệp để lưu điểm cao nhất
        static int score = 0;
        static int removedLines = 0; // Biến để đếm số dòng đã xóa
        static int levelremovelines;
        static int level = 1;

        // Giữ các biến
        const int holdSizeX = 6;
        const int holdSizeY = mapSizeY;
        static int holdIndex = -1;
        static char holdChar;

        const int upNextSize = 6;


        static ConsoleKeyInfo input;


        // Thông tin hiện tại
        static int currentX = 0;
        static int currentY = 0;
        static char currentChar = 'O';

        static int currentRot = 0;



        // Dãy các khối ngẫu nhiên
        static int[] bag;
        static int[] nextBag;

        static int bagIndex;
        static int currentIndex;


        // khác
        static Stopwatch stopwatch = new Stopwatch(); // Đo thời gian chơi thêm vào
        static int maxTime = 20;
        static int timer = 0;
        static int amount = 0;

        static int movesUsed = 0;

        static bool IsTitleScreen = true;



        //--------------------------------hằng số và các biến của trò chơi-------------------------------------//

        #endregion


        #region Vòng Lặp Chính Của Game

        //--------------------------------Vòng lặp game----------------------------------------------//
        static void Main()
        {
            Console.CursorVisible = false;
            TitleScreen();
            TransitionScreen();
            LoadHighScore();
            
            blockColours = new ConsoleColor[7];
            Console.Title = "doannhom8";
            DrawBorderingame();
            // Chạy inputThread để nhận input trực tiếp từ người chơi
            Thread inputThread = new Thread(Input);
            inputThread.Start();
            // Tạo dãy các khối tetrominos
            bag = GenerateBag();
            nextBag = GenerateBag();
            NewBlock();

            stopwatch.Start();
            while (true)
            {
                if (timer >= maxTime)
                {
                    //Nếu không va chạm, chỉ cần di chuyển tetromino xuống. Nếu có va chạm gọi hàm BlockDownCollision
                    if (!Collision(currentIndex, bg, currentX, currentY + 1, currentRot))
                    {
                        currentY++;
                    }
                    else
                    {
                        BlockDownCollision(); // tăng số biến movesUsed (số bước đã dùng) trong chế độ Puzzle
                    }

                    timer = 0;
                }
                timer++;

                // INPUT
                InputHandler(); // Gọi hàm InputHandler (xử lý input)
                input = new ConsoleKeyInfo(); // Reset input 



                char[,] view = RenderView(); // Kết xuất đồ họa cho màn hình chơi chính (Playing field)


                char[,] hold = RenderHold(); // Kết xuất đồ họa cho những tetromino được giữ lại 



                char[,] next = RenderUpNext(); // Kết xuất đồ họa cho 3 tetrominos tiếp theo trên màn hình chơi


                Print(view, hold, next); // In toàn bộ những đồ họa được kết xuất ra màn hình

                Thread.Sleep(10); // Chờ để không làm quá tải bộ xử lý (như vậy sẽ tốt hơn vì nó không ảnh hưởng gì đến cảm giác chơi game)
            }

        }
        //---------------------------------Vòng lặp game----------------------------------------------//

        #endregion


        #region Reset Trạng Thái Game
        static void ResetGameState()
        {
            // Reset các biến của game
            score = 0;
            removedLines = 0;
            level = 1;
            levelremovelines = 0;
            bagIndex = 0;
            holdIndex = -1;
            holdChar = ' ';
            amount = 0;
            maxTime = 22;
            currentRot = 0;
            currentX = 4;
            currentY = 0;

            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    bg[y, x] = '-';
                    blockColoursOnScreen[y, x] = ConsoleColor.DarkGray;
                }
            }

            // màu cho bóng của các khối tetrominos
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    shadowColorsOnScreen[y, x] = ConsoleColor.DarkGray;
                }
            }

            // Màu của khu vực giữ các khối tetrominos
            holdColour = ConsoleColor.DarkGray;

            // Màu cho khu vực hiện những khối tiếp theo
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < upNextSize; x++)
                {
                    nextBlockColours[y, x] = ConsoleColor.DarkGray;
                }
            }

            // tạo bag (dãy các tetrominos) mới
            bag = GenerateBag();
            nextBag = GenerateBag();

            // Khởi động lại stopwatch
            stopwatch.Restart();
        }

        #endregion


        #region Game Mode thứ 3: Classic With Blocks
        static void SetupClassicWithBlocks(ClWBDifficulty difficulty)
        {
            ResetGameState();
            Console.Clear();
            gameMode = GameMode.ClassicWithBlocks;

            Random random = new Random();
            int blockHeight = 0;

            //Chọn độ khó cho game mode 3

            switch (difficulty)
            {
                case ClWBDifficulty.Easy:
                    blockHeight = (int)(mapSizeY * 0.4);
                    break;
                case ClWBDifficulty.Medium:
                    blockHeight = (int)(mapSizeY * 0.5);
                    break;
                case ClWBDifficulty.Hard:
                    blockHeight = (int)(mapSizeY * 0.6);
                    break;
            }

            // Tạo các khối ngẫu nhiên trong khu vực chơi
            for (int y = mapSizeY - blockHeight; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    if (random.NextDouble() < 0.5) // 50% khả năng sẽ đặt 1 khối
                    {
                        bg[y, x] = '#';
                        blockColoursOnScreen[y, x] = colours[random.Next(0, colours.Length)];
                    }
                    else
                    {
                        bg[y, x] = '-';
                        blockColoursOnScreen[y, x] = ConsoleColor.DarkGray;
                    }
                }
            }

            DrawBorderingame();

            // Vẽ lại bảng game để hiện những khối ngẫu nhiên
            char[,] view = RenderView();
            char[,] hold = RenderHold();
            char[,] next = RenderUpNext();
            Print(view, hold, next);

            NewBlock();
            PlaySound(gamePlayer, @"C:\TetrisMusic\Main.wav", true);
        }

        static void SelectClassicWithBlocks()
        {
            Console.Clear();
            Console.SetCursorPosition(1, 1);
            DrawBorder();

            string[] difficultyMenu = new string[]
            {
                "+---------------------+",
                "|  Select Difficulty  |",
                "| 1. Easy             |",
                "| 2. Medium           |",
                "| 3. Hard             |",
                "| Esc to Return       |",
                "+---------------------+"
            };

            int startY = 14 - difficultyMenu.Length;
            foreach (string line in difficultyMenu)
            {
                int startX = 34 - line.Length;
                Console.SetCursorPosition(startX, startY++);
                Console.WriteLine(line);
            }

            //Đọc các input từ người chơi

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                        SetupClassicWithBlocks(ClWBDifficulty.Easy);
                        return;
                    case ConsoleKey.D2:
                        SetupClassicWithBlocks(ClWBDifficulty.Medium);
                        return;
                    case ConsoleKey.D3:
                        SetupClassicWithBlocks(ClWBDifficulty.Hard);
                        return;
                    case ConsoleKey.Escape:
                        TitleScreen();
                        return;
                }
            }
        }

        static void ClBWGameOver()
        {
            StopSound(gamePlayer);
            PlaySound(effectPlayer, @"C:\TetrisMusic\GameOver.wav", false);

            // Cập nhật số điểm cao nhất nếu cần thiết
            UpdateHighScore(score);


            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;
            int centerY = windowHeight / 2 - 3;

            // Hiện màn hình Game Over (sau khi thua)
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.WriteLine("|     GAME OVER      |");
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine("| Your Score: " + score.ToString("D4") + "   |");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine("| High Score: " + highScore.ToString("D4") + "   |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("| Play again? (Y/N)  |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y)
                {
                    StopSound(effectPlayer);
                    SelectClassicWithBlocks();
                    break;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    StopSound(effectPlayer);
                    TitleScreen(); // Quay lại màn hình chính thay vì thoát hoàn toàn chương trình đang chạy
                    break;
                }
            }
        }

        #endregion


        #region Game Mode thứ 2: Puzzle
        static void SelectPuzzleDifficulty()
        {
            Console.Clear();
            Console.SetCursorPosition(1, 1);
            DrawBorder();

            string[] difficultyMenu = new string[]
            {
                "+---------------------+",
                "|  Select Difficulty  |",
                "| 1. Easy             |",
                "| 2. Medium           |",
                "| 3. Hard             |",
                "| Esc to Return       |",
                "+---------------------+"
            };

            int startY = 14 - difficultyMenu.Length;
            foreach (string line in difficultyMenu)
            {
                int startX = 34 - line.Length;
                Console.SetCursorPosition(startX, startY++);
                Console.WriteLine(line);
            }

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                        SetupPuzzleMode(PuzzleDifficulty.Easy);
                        return;
                    case ConsoleKey.D2:
                        SetupPuzzleMode(PuzzleDifficulty.Medium);
                        return;
                    case ConsoleKey.D3:
                        SetupPuzzleMode(PuzzleDifficulty.Hard);
                        return;
                    case ConsoleKey.Escape:
                        TitleScreen();
                        return;
                }
            }
        }

        // Tạo hàm để điều chỉnh chế độ Puzzle
        static void SetupPuzzleMode(PuzzleDifficulty difficulty)
        {
            ResetGameState();
            Console.Clear();
            gameMode = GameMode.Puzzle;
            puzzleDifficulty = difficulty;
            movesUsed = 0; // Reset lại số bước dùng (biến movesUsed) khi bắt đầu một game puzzle mới


            //Các điều kiện của từng độ khó

            switch (difficulty)
            {
                case PuzzleDifficulty.Easy:
                    movesRemaining = 20;
                    targetLines = 3;
                    break;
                case PuzzleDifficulty.Medium:
                    movesRemaining = 15;
                    targetLines = 4;
                    break;
                case PuzzleDifficulty.Hard:
                    movesRemaining = 10;
                    targetLines = 5;
                    break;
            }

            DrawBorderingame();
            NewBlock();
            PlaySound(gamePlayer, @"C:\TetrisMusic\Main.wav", true);
        }

        static void PuzzleGameOver(bool won)
        {
            StopSound(gamePlayer);
            if (won)
            {
                PlaySound(effectPlayer, @"C:\TetrisMusic\GameWon.wav", false);
            }
            else
            {
                PlaySound(effectPlayer, @"C:\TetrisMusic\GameOver.wav", false);
            }

            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;
            int centerY = windowHeight / 2 - 3;

            //Hiển thị màn hình sau khi hoàn thành puzzle

            string resultMessage = won ? "PUZZLE COMPLETE!" : " PUZZLE FAILED! ";
            ConsoleColor messageColor = won ? ConsoleColor.Green : ConsoleColor.Red;

            // Vẽ màn hình game over hoặc game won
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.ForegroundColor = messageColor;
            Console.WriteLine($"|  {resultMessage}  |");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine($"| Lines: {removedLines}/{targetLines}         |");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine($"| Moves Used: {movesUsed:D2}     |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("| Play again? (Y/N)  |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (char.ToUpper(key.KeyChar))
                {
                    case 'Y':
                        StopSound(effectPlayer);
                        SelectPuzzleDifficulty();
                        return;
                    case 'N':
                        StopSound(effectPlayer);
                        TitleScreen(); // Quay lại màn hình chính
                        return;
                }
            }
        }

        #endregion

        #region Hàm dùng để tạo thêm bóng cho các khối Tetrominos
        static int GetShadowY()
        {
            int shadowY = currentY;
            while (!Collision(currentIndex, bg, currentX, shadowY + 1, currentRot))
            {
                shadowY++;
            }
            return shadowY;
        }

        #endregion

        #region Màn hình chính và màn hình chuyển tiếp
        static void TransitionScreen()
        {
            StopSound(titlePlayer); // dừng nhạc nền
            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 22;
            int centerX = windowWidth / 2 - 10;
            int centerY = windowHeight / 2 - 3;

            int barWidth = 20;
            string emptyBar = new string(' ', barWidth);
            string fullBar = new string('#', barWidth);

            for (int i = 0; i <= barWidth; i++)
            {
                string currentBar = new string('#', i) + new string(' ', barWidth - i);
                Console.CursorVisible = false;
                Console.SetCursorPosition(centerX, centerY);
                Console.WriteLine("+----------------------+");
                Console.SetCursorPosition(centerX, centerY + 1);
                Console.WriteLine("|      LOADING...      |");
                Console.SetCursorPosition(centerX, centerY + 2);
                Console.WriteLine($"|[{currentBar}]| ");
                Console.SetCursorPosition(centerX, centerY + 3);
                Console.WriteLine("|                      |");
                Console.SetCursorPosition(centerX, centerY + 4);
                Console.WriteLine("+----------------------+");

                Thread.Sleep(100);
            }

            for (int i = 3; i > 0; i--)
            {
                Console.SetCursorPosition(centerX, centerY);
                Console.WriteLine("+----------------------+");
                Console.SetCursorPosition(centerX, centerY + 1);
                Console.WriteLine($"|    Starting in {i}...  |");
                Console.SetCursorPosition(centerX, centerY + 2);
                Console.WriteLine("|                      |");
                Console.SetCursorPosition(centerX, centerY + 3);
                Console.WriteLine("|     GET  READY!      |");
                Console.SetCursorPosition(centerX, centerY + 4);
                Console.WriteLine("+----------------------+");

                Thread.Sleep(1000);
            }

            Console.Clear();

            // Chạy nhạc game sau khi màn hình chuyển tiếp đã chạy xong
            PlaySound(gamePlayer, @"C:\TetrisMusic\Main.wav", true);
        }

        static void TitleScreen()
        {

            bool skipAnimation = false;
            PlaySound(titlePlayer, @"C:\TetrisMusic\Menu.wav", true);

            IsTitleScreen = true;

            Console.Clear();
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            DrawBorder();

            string[] tetrisArt = new string[]
            {
        "TTTTTT EEEEEE TTTTTT  RRRRR   IIII  SSSSS",
        "  TT   EE       TT    R    R   II   SS    ",
        "  TT   EEEE     TT    RRRRR    II   SSSSS",
        "  TT   EE       TT    R  RR    II      SS",
        "  TT   EEEEEE   TT    R    RR IIII  SSSSS"
            };

            int windowWidth = 40;
            int windowHeight = 20;
            int artStartY = 4;

            // Display the TETRIS art

            ConsoleKeyInfo keyInfo;

            // Check for skip input before starting animations
            if (Console.KeyAvailable)
            {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Spacebar)
                {
                    skipAnimation = true;
                }
            }

            // Tetris art animation
            for (int i = 0; i < tetrisArt.Length; i++)
            {
                int artWidth = tetrisArt[i].Length;
                int startX = Math.Max(1, (windowWidth - artWidth) / 2);
                Console.SetCursorPosition(startX, artStartY + i);

                if (skipAnimation)
                {
                    Console.Write(tetrisArt[i]);
                }
                else
                {
                    foreach (char c in tetrisArt[i])
                    {
                        // Check for skip input during animation
                        if (Console.KeyAvailable)
                        {
                            keyInfo = Console.ReadKey(true);
                            if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Spacebar)
                            {
                                skipAnimation = true;
                                Console.Write(tetrisArt[i].Substring(Console.CursorLeft - startX));
                                break;
                            }
                        }
                        Console.Write(c);
                        Thread.Sleep(5);
                    }
                }
            }

            // Menu lines animation
            string[] menuLines = new string[]
            {
    "+-----------------------+",
    "| 1. Classic Mode       |",
    "| 2. Puzzle Mode        |",
    "| 3. Classic with Blocks|",
    "| 4. Instructions       |",
    "| Esc to Exit           |",
    "+-----------------------+"
            };

            int menuStartY = 11;
            foreach (string line in menuLines)
            {
                int menuX = Math.Max(1, (45 - line.Length) / 2);
                Console.SetCursorPosition(menuX, menuStartY++);

                if (skipAnimation)
                {
                    Console.Write(line);
                }
                else
                {
                    foreach (char c in line)
                    {
                        // Check for skip input during animation
                        if (Console.KeyAvailable)
                        {
                            keyInfo = Console.ReadKey(true);
                            if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Spacebar)
                            {
                                skipAnimation = true;
                                Console.Write(line.Substring(Console.CursorLeft - menuX));
                                break;
                            }
                        }
                        Console.Write(c);
                        Thread.Sleep(10);
                    }
                }
            }

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.D1)
                {
                    ResetGameState();
                    gameMode = GameMode.Classic;
                    IsTitleScreen = false;
                    Console.Clear();
                    DrawBorderingame();
                    NewBlock();
                    PlaySound(gamePlayer, @"C:\TetrisMusic\Main.wav", true);
                    break;
                }
                else if (key.Key == ConsoleKey.D2)
                {
                    ResetGameState();
                    SelectPuzzleDifficulty();
                    IsTitleScreen = false;
                    break;
                }
                else if (key.Key == ConsoleKey.D3)
                {
                    ResetGameState();
                    SelectClassicWithBlocks();
                    IsTitleScreen = false;
                    break;
                }
                else if (key.Key == ConsoleKey.D4)
                {
                    ShowInstructions();
                    break;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    esc();
                }
            }
        }

        #endregion


        #region Xử Lý Input từ Người Chơi
        static void InputHandler()
        {
            switch (input.Key)
            {
                // Nút A/bàn phím trái = di chuyển khối sang trái
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    if (!Collision(currentIndex, bg, currentX - 1, currentY, currentRot)) currentX -= 1;
                    break;

                // Nút D/bàn phím phải = di chuyển khối sang phải
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    if (!Collision(currentIndex, bg, currentX + 1, currentY, currentRot)) currentX += 1;
                    break;

                // Nút w/bàn phím lên = xoay khối
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    int newRot = currentRot + 1;
                    if (newRot >= 4) newRot = 0;
                    if (!Collision(currentIndex, bg, currentX, currentY, newRot)) currentRot = newRot;

                    break;

                // Phím cách (spacebar) = hard drop (khối sẽ chạy thẳng xuối dưới ngay lập tức)
                case ConsoleKey.Spacebar:
                    int i = 0;
                    while (true)
                    {
                        i++;
                        if (Collision(currentIndex, bg, currentX, currentY + i, currentRot))
                        {
                            currentY += i - 1;
                            BlockDownCollision(); // sẽ tăng số bước đã dùng cho chế độ puzzle
                            break;
                        }
                    }
                    break;


                // Nút esc = Thoát
                case ConsoleKey.Escape:
                    esc();
                    break;

                // Nút Enter = giữ khối
                case ConsoleKey.Enter:

                    // nếu hiện tại không có khối nào
                    if (holdIndex == -1)
                    {
                        holdIndex = currentIndex;
                        holdChar = currentChar;
                        NewBlock();
                    }
                    // nếu có khối đang giữ
                    else
                    {
                        if (!Collision(holdIndex, bg, currentX, currentY, 0)) // kiểm tra xem có va chạm không
                        {

                            // thay đổi khối hiện tại sang khối được giữ
                            int c = currentIndex;
                            char ch = currentChar;
                            currentIndex = holdIndex;
                            currentChar = holdChar;
                            holdIndex = c;
                            holdChar = ch;
                        }

                    }
                    break;

                // Nút S/bàn phím xuống = di chuyển khối đi xuống nhanh hơn (soft drop)
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (!Collision(currentIndex, bg, currentX, currentY + 1, currentRot))
                    {
                        currentY++;
                    }
                    else
                    {
                        BlockDownCollision(); // sẽ tăng số bước đã dùng cho chế độ puzzle
                    }
                    break;

            }

        }

        #endregion




        #region Màn Hình Thoát Game
        static void esc()
        {
            Console.Clear();
            DrawBorder();
            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;  // Tâm X để căn giữa thông báo
            int centerY = windowHeight / 2 - 3;  // Tâm Y để căn giữa thông báo

            // In thông báo Game Over
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.WriteLine("|   DO YOU WANT TO   |");
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("|   EXIT THE GAME?   |");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine("|   YES OR NO(Y/N)   |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("|                    |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            // chờ input từ người chơi
            ConsoleKeyInfo keyInfo;
            do
            {

                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Y)
                {
                    if (IsTitleScreen)
                    {
                        Environment.Exit(0); // Thoát hoàn toàn nếu đang ở màn hình chính
                    }
                    else
                    {
                        TitleScreen(); // Quay lại màn hình chính nếu đang trong trò chơi
                    }
                }
                else if (keyInfo.Key == ConsoleKey.N)
                {
                    if (IsTitleScreen)
                    {
                        TitleScreen();
                    }
                    else
                    {
                        Console.Clear();
                        DrawBorderingame();
                        return; // Tiếp tục trò chơi
                    }
                }
            }
            while (keyInfo.Key != ConsoleKey.Y && keyInfo.Key != ConsoleKey.N);
        }

        #endregion


        #region Khởi động lại
        static void Restart()
        {
            ResetGameState();
            Console.Clear();

            if (gameMode == GameMode.Classic)
            {
                DrawBorderingame();
                NewBlock();
            }
            else if (gameMode == GameMode.Puzzle)
            {
                SelectPuzzleDifficulty();
            }
            else if (gameMode == GameMode.ClassicWithBlocks)
            {
                SelectClassicWithBlocks();
            }

            PlaySound(gamePlayer, @"C:\TetrisMusic\Main.wav", true);
        }

        #endregion


        #region Số dòng đã dọn

        static void ClearLine(int lineY)
        {
            score += 40;
            removedLines++;
            levelremovelines++;
            // dọn số dòng
            for (int x = 0; x < mapSizeX; x++)
            {
                bg[lineY, x] = '-';
                blockColoursOnScreen[lineY, x] = ConsoleColor.DarkGray;
            }

            //vòng lặp qua số dòng
            for (int y = lineY - 1; y > 0; y--)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                   
                    char character = bg[y, x];
                    if (character != '-')
                    {
                        bg[y, x] = '-';
                        bg[y + 1, x] = character;
                        blockColoursOnScreen[y + 1, x] = blockColoursOnScreen[y, x];
                        blockColoursOnScreen[y, x] = ConsoleColor.DarkGray;
                    }

                }
            }
        }

        #endregion


        #region Render cho Game
        static char[,] RenderView()
        {
            char[,] view = new char[mapSizeY, mapSizeX];

            // Tạo khung nhìn bằng với màn hình hiện tại
            for (int y = 0; y < mapSizeY; y++)
                for (int x = 0; x < mapSizeX; x++)
                    view[y, x] = bg[y, x];

            // Tính toán vị trị bóng của các khối
            int shadowY = GetShadowY();

            // tạo hình ảnh cho bóng của các khối trước
            if (shadowY != currentY)
            {
                for (int i = 0; i < positions.GetLength(2); i++)
                {
                    int x = positions[currentIndex, currentRot, i, 0] + currentX;
                    int y = positions[currentIndex, currentRot, i, 1] + shadowY;
                    if (y >= 0 && y < mapSizeY && x >= 0 && x < mapSizeX)
                    {
                        if (view[y, x] == '-')  // chỉ tạo ảnh cho bóng nếu khu vực này trống
                        {
                            view[y, x] = '░';  // tạo bóng
                                               // lưu trữ màu cho bóng của các khối
                            shadowColorsOnScreen[y, x] = ConsoleColor.DarkGray;
                        }
                    }
                }
            }

            
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                int x = positions[currentIndex, currentRot, i, 0] + currentX;
                int y = positions[currentIndex, currentRot, i, 1] + currentY;
                if (y >= 0 && y < mapSizeY && x >= 0 && x < mapSizeX)
                {
                    view[y, x] = '#';
                    blockColoursOnScreen[y, x] = blockColours[currentIndex];
                }
            }
            return view;
        }

        static ConsoleColor[,] shadowColorsOnScreen = new ConsoleColor[mapSizeY, mapSizeX];


        static char[,] RenderHold()
        {
            char[,] hold = new char[holdSizeY, holdSizeX];
            // giữ khối (bằng một array rỗng)
            for (int y = 0; y < holdSizeY; y++)
                for (int x = 0; x < holdSizeX; x++)
                    hold[y, x] = ' ';


            // Nếu đang giữ khối
            if (holdIndex != -1)
            {
                // Ghi đè lên khối đang giữ
                for (int i = 0; i < positions.GetLength(2); i++)
                {
                    hold[positions[holdIndex, 0, i, 1] + 1, positions[holdIndex, 0, i, 0] + 1] = '#';
                }
                holdColour = blockColours[holdIndex];
            }
            return hold;
        }

        static ConsoleColor holdColour;
        static char[,] RenderUpNext()
        {
            
            char[,] next = new char[mapSizeY, upNextSize];
            for (int y = 0; y < mapSizeY; y++)
                for (int x = 0; x < upNextSize; x++)
                    next[y, x] = ' ';


            
            // Gán màu sắc cho các khối trong phần chuẩn bị xuất hiện
            int nextBagIndex = 0;
            for (int i = 0; i < 3; i++) // 3 khối tiếp theo
            {
                for (int l = 0; l < positions.GetLength(2); l++)
                {
                    if (i + bagIndex >= 7) // Nếu thực sự cần truy cập vào bag (dãy các khối ngẫyu nhiên) tiếp theo
                    {
                        next[positions[nextBag[nextBagIndex], 0, l, 1] + 5 * i, positions[nextBag[nextBagIndex], 0, l, 0] + 1] = '#';
                        nextBlockColours[positions[nextBag[nextBagIndex], 0, l, 1] + 5 * i, positions[nextBag[nextBagIndex], 0, l, 0] + 1] = blockColours[nextBag[nextBagIndex]];
                    }
                    else
                    {
                        next[positions[bag[bagIndex + i], 0, l, 1] + 5 * i, positions[bag[bagIndex + i], 0, l, 0] + 1] = '#';
                        nextBlockColours[positions[bag[bagIndex + i], 0, l, 1] + 5 * i, positions[bag[bagIndex + i], 0, l, 0] + 1] = blockColours[bag[bagIndex + i]];
                    }
                }
                if (i + bagIndex >= 7) nextBagIndex++;
            }

            return next;
        }

        static ConsoleColor[,] nextBlockColours = new ConsoleColor[mapSizeY, upNextSize];
        #endregion

        #region Hàm để in các giao diện
        static void Print(char[,] view, char[,] hold, char[,] next)
        {
            Level();
            Console.ForegroundColor = ConsoleColor.Gray; // Reset màu cho viền 

            for (int y = 0; y < mapSizeY; y++)
            {
                Console.SetCursorPosition(1, y + 1);
                for (int x = 0; x < holdSizeX + mapSizeX + upNextSize; x++)
                {
                    char i = ' ';
                    if (x < holdSizeX) i = hold[y, x];
                    else if (x >= holdSizeX + mapSizeX) i = next[y, x - mapSizeX - upNextSize];
                    else i = view[y, (x - holdSizeX)];

                    // xử lý màu
                    if (x >= holdSizeX && x < holdSizeX + mapSizeX)
                    {
                        int gameX = x - holdSizeX;
                        if (i == '#')
                        {
                            Console.ForegroundColor = blockColoursOnScreen[y, gameX];
                        }
                        else if (i == '░')
                        {
                            Console.ForegroundColor = shadowColorsOnScreen[y, gameX];
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                        }
                    }
                    else if (i == '#')
                    {
                        if (x < holdSizeX)
                        {
                            Console.ForegroundColor = holdColour;
                        }
                        else
                        {
                            int indexX = x - holdSizeX - mapSizeX;
                            Console.ForegroundColor = nextBlockColours[y, indexX];
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }
                    Console.Write(i);
                }

                Console.ForegroundColor = ConsoleColor.Gray; // Reset màu cho dòng chữ trạng thái hiện thái
                if (gameMode == GameMode.Classic || gameMode == GameMode.ClassicWithBlocks)
                {
                    if (y == 1) Console.Write($"| High Score: {highScore}  ");
                    if (y == 3) Console.Write($"| Score: {score}  ");
                    if (y == 5) Console.Write($"| Lines: {removedLines} ");
                    if (y == 7) Console.Write($"| Time: {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:D2} ");
                    if (y == 9) Console.Write($"| Level: {level} ");
                }
                else // trong chế độ puzzle
                {
                    if (y == 1) Console.Write($"| Difficulty: {puzzleDifficulty}");
                    if (y == 3) Console.Write($"| Moves Left: {movesRemaining}    ");
                    if (y == 5) Console.Write($"| Lines: {removedLines}/{targetLines}     ");
                    if (y == 7) Console.Write($"| Time: {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:D2} ");
                }
                Console.WriteLine();
            }
        }

        #endregion

        #region Thuật toán để tạo các khối Tetris ngẫu nhiên
        static int[] GenerateBag()
        {
            //source: https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
            Random random = new Random();
            int n = 7;
            int[] ret = { 0, 1, 2, 3, 4, 5, 6 };
            while (n > 1)
            {
                int k = random.Next(n--);
                int temp = ret[n];
                ret[n] = ret[k];
                ret[k] = temp;

            }
            return ret;

        }

        #endregion


        #region Các hàm xử lý khác
        static void BlockDownCollision()
        {
            // Thêm khối từ current vào background (màn hình)
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                int x = positions[currentIndex, currentRot, i, 0] + currentX;
                int y = positions[currentIndex, currentRot, i, 1] + currentY;
                if (y >= 0 && y < mapSizeY && x >= 0 && x < mapSizeX)
                {
                    bg[y, x] = '#';
                    blockColoursOnScreen[y, x] = blockColours[currentIndex];
                }
            }

            if (gameMode == GameMode.Puzzle)
            {
                movesUsed++; // tăng số lượng bước đã sử dụng khi đặt một khối tetromino xuống
                movesRemaining--;
            }

            // Kiểm tra các dòng
            bool linesCleared = false;
            int linesThisMove = 0;
            while (true)
            {
                int lineY = Line(bg);
                if (lineY != -1)
                {
                    ClearLine(lineY);
                    linesCleared = true;
                    linesThisMove++;
                    continue;
                }
                break;
            }

            // kiểm tra điều kiện thắng/thua cho chế độ puzzle
            if (gameMode == GameMode.Puzzle)
            {


                if (removedLines >= targetLines)
                {
                    PuzzleGameOver(true);
                    return;
                }
                else if (movesRemaining <= 0)
                {
                    PuzzleGameOver(false);
                    return;
                }
            }

            NewBlock();
        }
        static ConsoleColor[,] blockColoursOnScreen = new ConsoleColor[mapSizeY, mapSizeX];
        static bool Collision(int index, char[,] bg, int x, int y, int rot)
        {

            for (int i = 0; i < positions.GetLength(2); i++)
            {
                // kiểm tra xem có bị lỗi ngoài khu vực chơi không (out of bound)
                if (positions[index, rot, i, 1] + y >= mapSizeY || positions[index, rot, i, 0] + x < 0 || positions[index, rot, i, 0] + x >= mapSizeX)
                {
                    return true;
                }

                if (bg[positions[index, rot, i, 1] + y, positions[index, rot, i, 0] + x] != '-')
                {
                    return true;
                }
            }

            return false;
        }

        static int Line(char[,] bg)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                bool i = true;
                for (int x = 0; x < mapSizeX; x++)
                {
                    if (bg[y, x] == '-')
                    {
                        i = false;
                    }
                }
                if (i) return y;
            }

            // nếu không có dòng nào 
            return -1;
        }

        static void NewBlock()
        {
            try
            {
                // kiểu tra xem liệu có cần tạo thêm dãy các tetromino mới không
                if (bagIndex >= 7)
                {
                    bagIndex = 0;
                    bag = nextBag;
                    nextBag = GenerateBag();
                }

                // Reset tất cả
                currentY = 0;
                currentX = 4;
                currentChar = '#';
                currentIndex = bag[bagIndex];

                // Gán màu sắc cho khối mới
                if (currentIndex >= 0 && currentIndex < colours.Length)
                {
                    blockColours[currentIndex] = colours[currentIndex];
                }

                // Gán màu sắc cho các khối trong phần chuẩn bị xuất hiện
                for (int i = 0; i < 3; i++)
                {
                    if (i + bagIndex >= 7)
                    {
                        if (i - (7 - bagIndex) >= 0 && i - (7 - bagIndex) < blockColours.Length)
                        {
                            blockColours[nextBag[i - (7 - bagIndex)]] = colours[nextBag[i - (7 - bagIndex)]];
                        }
                    }
                    else
                    {
                        if (bagIndex + i >= 0 && bagIndex + i < blockColours.Length)
                        {
                            blockColours[bag[bagIndex + i]] = colours[bag[bagIndex + i]];
                        }
                    }
                }

                // kiểm tra nếu bị collision (tức là khi các khối chạm đỉnh của màn hình game) thì sẽ thua (gọi hàm GameOver())
                if (Collision(currentIndex, bg, currentX, currentY, currentRot) && amount > 0)
                {
                    if (gameMode == GameMode.Puzzle)
                    {
                        PuzzleGameOver(false);
                    }
                    else if (gameMode == GameMode.Classic || gameMode == GameMode.ClassicWithBlocks)
                    {
                        GameOver();
                    }
                }
                bagIndex++;
                amount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when creating new block: " + ex.Message);
            }
        }

        #endregion

        #region Vẽ Viền Cho Game
        static void DrawBorder()
        {
            int width = 40;
            int height = 20;

            // Vẽ viền trên
            Console.SetCursorPosition(0, 0);
            Console.Write("+" + new string('-', width + 2) + "+");

            // Vẽ các cạnh
            for (int i = 1; i < height + 1; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("|");

                Console.SetCursorPosition(width + 3, i);
                Console.Write("|");
            }

            // Vẽ viền dưới
            Console.SetCursorPosition(0, height + 1);
            Console.Write("+" + new string('-', width + 2) + "+");
        }
        static void DrawBorderingame()
        {
            int width = 40;
            int height = 20;

            // Vẽ viền trên
            Console.SetCursorPosition(0, 0);
            Console.Write("+" + new string('-', width + 2) + "+");

            // Vẽ các cạnh
            for (int i = 1; i < height + 1; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("|");
                Console.SetCursorPosition(width - 17, i);
                Console.Write("|");
                Console.SetCursorPosition(width + 3, i);
                Console.Write("|");
            }
            Console.SetCursorPosition(23, height - 8);
            Console.Write("+" + new string('-', 19) + "+");
            // Vẽ viền dưới
            Console.SetCursorPosition(0, height + 1);
            Console.Write("+" + new string('-', width + 2) + "+");
        }

        #endregion

        #region Xử lý level (cấp độ)
        static void Level()//themvao
        {
            if (levelremovelines >= 5 && level <= 10)
            {
                level++;
                maxTime -= 2;
                levelremovelines -= 5;
            }
        }
        #endregion


        #region Đọc điểm từ tệp
        // Phương thức đọc điểm cao nhất từ tệp
        static void LoadHighScore()
        {
            try
            {
                if (File.Exists(highScoreFile))
                {
                    string scoreText = File.ReadAllText(highScoreFile);
                    int.TryParse(scoreText, out highScore); // Chuyển đổi giá trị trong tệp thành số
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when creating high score: " + ex.Message);
            }
        }
        // Phương thức cập nhật điểm cao nhất
        static void UpdateHighScore(int currentScore)
        {
            if (currentScore > highScore)
            {
                highScore = currentScore;
                File.WriteAllText(highScoreFile, highScore.ToString()); // Lưu điểm cao nhất vào tệp
            }
        }

        #endregion

        #region Màn hình sau khi thua (Game Over)
        static void GameOver()
        {
            StopSound(gamePlayer);
            PlaySound(effectPlayer, @"C:\TetrisMusic\GameOver.wav", false);

            // Cập nhật số điểm cao nhất nếu cần thiết
            UpdateHighScore(score);

            // Làm sạch console và vẽ viền
            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;
            int centerY = windowHeight / 2 - 3;

            // Hiển thị màn hình sau khi thua cuộc 
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.WriteLine("|     GAME OVER      |");
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine("| Your Score: " + score.ToString("D4") + "   |");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine("| High Score: " + highScore.ToString("D4") + "   |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("| Play again? (Y/N)  |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y)
                {
                    StopSound(effectPlayer);
                    Restart();
                    break;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    StopSound(effectPlayer);
                    TitleScreen(); // Quay lại màn hình chính
                    break;
                }
            }
        }
        #endregion

        #region Đọc Input
        static void Input()
        {
            while (true)
            {
                // Lấy input từ bàn phím
                input = Console.ReadKey(true);
            }
        }

        #endregion
    }

}
