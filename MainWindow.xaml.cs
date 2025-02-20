using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MySql.Data.MySqlClient;
using System.Data;
using System.Media;
using System.Windows.Media;
using Microsoft.Kinect;


namespace test1
{

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private SoundPlayer selectSound;
        private SoundPlayer boomSound;
        private MediaPlayer backgroundMusic;
        private IngredientFactory ingredientFactory;
        private List<Ingredient> fallingIngredients;
        private DispatcherTimer gameLoop;
        private int totalScore;
        private int gameTime;
        private Stack<Ingredient> burgerStack = new Stack<Ingredient>();
        private Stack<Image> burgerStackImages = new Stack<Image>();
        private string userName = UserManager.User.UserName;
        KinectSensor nui = null;
        public MainWindow()
        {

            InitializeComponent();
            InitializeNui();
            //전체화면
            this.WindowState = WindowState.Maximized;
            ingredientFactory = new IngredientFactory();
            fallingIngredients = new List<Ingredient>();
            totalScore = 0;
            gameTime = 60; // 시간 설정


            // 게임 루프 시작
            gameLoop = new DispatcherTimer();
            gameLoop.Tick += GameLoop_Tick;
            gameLoop.Interval = TimeSpan.FromSeconds(1); // 1초 간격으로 실행
            gameLoop.Start();

            //배경음악 루프
            backgroundMusic = new MediaPlayer();
            backgroundMusic.Open(new Uri("Sound/background.mp3", UriKind.Relative));
            backgroundMusic.MediaEnded += (s, e) => backgroundMusic.Position = TimeSpan.Zero;
            backgroundMusic.Play();

            //효과음
            selectSound = new SoundPlayer("Sound/water.wav");
            boomSound = new SoundPlayer("Sound/boom.wav");

        }
        void InitializeNui()
        {
          nui = KinectSensor.KinectSensors[0];
          //nui.ColorStream.Enable();
          nui.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(nui_ColorFrameReady);
          //
          //nui.DepthStream.Enable();
          nui.SkeletonStream.Enable();
          nui.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(nui_AllFramesReady);
          //
          nui.Start();
        }

        //손이 재료와 닿는지 안닿는지 확인
        bool IsHandTouchingIngredient(Point handPoint, Image ingredientImage)
        {
            double left = Canvas.GetLeft(ingredientImage);
            double top = Canvas.GetTop(ingredientImage);
            double right = left + ingredientImage.ActualWidth;
            double bottom = top + ingredientImage.ActualHeight;

            if (handPoint.X >= left && handPoint.X <= right && handPoint.Y >= top && handPoint.Y <= bottom)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void nui_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            ColorImageFrame ImageParam = e.OpenColorImageFrame();

            if (ImageParam == null) return;

            byte[] ImageBits = new byte[ImageParam.PixelDataLength];
            ImageParam.CopyPixelDataTo(ImageBits);

            BitmapSource src = BitmapSource.Create(
                ImageParam.Width,
                ImageParam.Height,
                96,
                96,
                PixelFormats.Bgr32,
                null,
                ImageBits,
                ImageParam.Width * ImageParam.BytesPerPixel);
            image2.Source = src;
        }
        void nui_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            SkeletonFrame sf = e.OpenSkeletonFrame();
            if (sf == null) return;
            Skeleton[] skeletonData = new Skeleton[sf.SkeletonArrayLength];
            sf.CopySkeletonDataTo(skeletonData);
            using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame != null)
                {
                    foreach (Skeleton sd in skeletonData)
                    {
                        if (sd.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // Left Hand
                            Joint leftHandJoint = sd.Joints[JointType.HandLeft];
                            DepthImagePoint leftHandDepthPoint = depthImageFrame.MapFromSkeletonPoint(leftHandJoint.Position);
                            Point leftHandPoint = new Point(
                                (int)(image1.Width * leftHandDepthPoint.X / depthImageFrame.Width),
                                (int)(image1.Height * leftHandDepthPoint.Y / depthImageFrame.Height));

                            // Right Hand
                            Joint rightHandJoint = sd.Joints[JointType.HandRight];
                            DepthImagePoint rightHandDepthPoint = depthImageFrame.MapFromSkeletonPoint(rightHandJoint.Position);
                            Point rightHandPoint = new Point(
                                (int)(image1.Width * rightHandDepthPoint.X / depthImageFrame.Width),
                                (int)(image1.Height * rightHandDepthPoint.Y / depthImageFrame.Height));

                            textBlock1.Text = string.Format("Left Hand: X:{0:0.00} Y:{1:0.00}\nRight Hand: X:{2:0.00} Y:{3:0.00}",
                                leftHandPoint.X, leftHandPoint.Y,
                                rightHandPoint.X, rightHandPoint.Y);

                            Canvas.SetLeft(ellipseLeftHand, leftHandPoint.X);
                            Canvas.SetTop(ellipseLeftHand, leftHandPoint.Y);

                            Canvas.SetLeft(ellipseRightHand, rightHandPoint.X);
                            Canvas.SetTop(ellipseRightHand, rightHandPoint.Y);
                            foreach (var ingredient in fallingIngredients)
                            {
                                if (IsHandTouchingIngredient(leftHandPoint, ingredient.Image) ||
                                    IsHandTouchingIngredient(rightHandPoint, ingredient.Image))
                                {
                                    // 손의 위치가 재료의 경계 내에 있다면, 해당 재료의 클릭 동작을 수행
                                    IngredientClicked(ingredient);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            gameTime--;

            // 화면에 남은 시간과 점수 업데이트
            timeText.Text = gameTime.ToString();
            scoreText.Text = totalScore.ToString();

            // 시간이 다 되면 게임 종료
            if (gameTime <= 0)
            {
                gameLoop.Stop();
                MessageBox.Show($"게임 종료! 총 점수: {totalScore}");
                EndGame();

                return;
            }

            // 랜덤한 재료 생성
            CreateFallingIngredient();

            // 모든 재료 아래로 이동
            AnimateFallingIngredients();
        }

        private void CreateFallingIngredient()
        {

            // 랜덤한 재료 생성 및 리스트에 추가
            Ingredient newIngredient = ingredientFactory.CreateRandomIngredient();
            fallingIngredients.Add(newIngredient);

            // 재료를 화면에 추가
            gameCanvas.Children.Add(newIngredient.Image);

            // 재료의 수가 너무 많아지면, 가장 오래된 재료를 제거
            if (fallingIngredients.Count > 10)
            {
                Ingredient oldestIngredient = fallingIngredients[0];
                gameCanvas.Children.Remove(oldestIngredient.Image);
                fallingIngredients.RemoveAt(0);
            }

            // 재료 클릭 이벤트 연결
            newIngredient.Image.MouseDown += (sender, e) =>
            {
                // 클릭된 재료를 처리하는 로직
                IngredientClicked(newIngredient);
            };

            // 재료의 초기 위치 설정
            Canvas.SetLeft(newIngredient.Image, new Random().Next(0, (int)gameCanvas.ActualWidth - (int)newIngredient.Image.ActualWidth));
            Canvas.SetTop(newIngredient.Image, 200);
        }
        void IngredientClicked(Ingredient clickedIngredient)
        {
            
                
                // 재료를 화면에서 제거
                gameCanvas.Children.Remove(clickedIngredient.Image);
                fallingIngredients.Remove(clickedIngredient);

                // 재료가 올바른 순서인지 확인
                if (burgerStack.Count == 0 && clickedIngredient is Bun ||
                    burgerStack.Count == 1 && clickedIngredient is Vegetable ||
                    burgerStack.Count == 2 && clickedIngredient is Patty ||
                    burgerStack.Count == 3 && clickedIngredient is Tomato ||
                    burgerStack.Count == 4 && clickedIngredient is Bun)
                {
                    // 재료를 스택에 추가
                    burgerStack.Push(clickedIngredient);

                    // 작은 이미지를 생성하여 스택에 추가
                    Image smallImage = new Image();
                    smallImage.Source = clickedIngredient.Image.Source;
                    smallImage.Width = 150;
                    smallImage.Height = 150;
                    Canvas.SetLeft(smallImage, 10);
                    Canvas.SetTop(smallImage, gameCanvas.ActualHeight - 50 * (burgerStackImages.Count + 5));
                    gameCanvas.Children.Add(smallImage);
                    burgerStackImages.Push(smallImage);
                    selectSound.Play();
                }
                else if
                    (burgerStack.Count == 0 && clickedIngredient is Bun ||
                    burgerStack.Count == 1 && clickedIngredient is Vegetable ||
                    burgerStack.Count == 2 && clickedIngredient is Shrimp ||
                    burgerStack.Count == 3 && clickedIngredient is Patty ||
                    burgerStack.Count == 4 && clickedIngredient is Bun)
            {
                    // 재료를 스택에 추가
                    burgerStack.Push(clickedIngredient);

                    // 작은 이미지를 생성하여 스택에 추가
                    Image smallImage = new Image();
                    smallImage.Source = clickedIngredient.Image.Source;
                    smallImage.Width = 150;
                    smallImage.Height = 150;
                    Canvas.SetLeft(smallImage, 10);
                    Canvas.SetTop(smallImage, gameCanvas.ActualHeight - 50 * (burgerStackImages.Count + 5));
                    gameCanvas.Children.Add(smallImage);
                    burgerStackImages.Push(smallImage);
                    selectSound.Play();
                }
                // 폭탄
                else if (burgerStack.Count >= 0 && clickedIngredient is Boom)
                {
                    // 점수 마이너스
                    totalScore -= 30;

                    // 스택을 비움
                    burgerStack.Clear();
                    while (burgerStackImages.Count > 0)
                    {
                        gameCanvas.Children.Remove(burgerStackImages.Pop());
                    }

                    boomSound.Play();
                }
                else 
                {
                    totalScore -= 10;
                    boomSound.Play();
                //    // 재료가 잘못된 순서라면 스택을 비움
                //    burgerStack.Clear();
                //    boomSound.Play();
                //    while (burgerStackImages.Count > 0)
                //    {
                //        gameCanvas.Children.Remove(burgerStackImages.Pop());
                //
                //    }
                 }

                // 햄버거가 완성되었는지 확인
                if (burgerStack.Count == 5)
                {
                    // 완성된 햄버거의 점수 추가
                    totalScore += 100;

                    // 스택을 비움
                    burgerStack.Clear();
                    while (burgerStackImages.Count > 0)
                    {
                        gameCanvas.Children.Remove(burgerStackImages.Pop());
                    }
                }
            }
         

        private void AnimateFallingIngredients()
        {
            for (int i = fallingIngredients.Count - 1; i >= 0; i--)
            {
                var ingredient = fallingIngredients[i];

                // 재료를 아래로 이동
                Canvas.SetTop(ingredient.Image, Canvas.GetTop(ingredient.Image) + 10);

                // 재료가 화면 아래로 사라지면 리스트에서 제거
                if (Canvas.GetTop(ingredient.Image) >= gameCanvas.ActualHeight)
                {
                    gameCanvas.Children.Remove(ingredient.Image);
                    fallingIngredients.RemoveAt(i);
                }
            }
        }

        private void EndGame()
        {
            try
            {
                // 점수를 데이터베이스에 저장
                using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=GameDB;Uid=root;Pwd=rootpw;"))
                {
                    connection.Open();

                    using (MySqlCommand command = new MySqlCommand("INSERT INTO Scores (UserName, Score) VALUES (@UserName, @Score)", connection))
                    {
                        command.Parameters.AddWithValue("@UserName", userName);
                        command.Parameters.AddWithValue("@Score", totalScore);

                        command.ExecuteNonQuery();
                    }

                    // 전체 순위를 표시
                    using (MySqlCommand command = new MySqlCommand("SELECT UserName, Score FROM Scores ORDER BY Score DESC", connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            StringBuilder ranking = new StringBuilder();
                            int rank = 1;
                            while (reader.Read())
                            {
                                string dbUserName = reader.GetString(0);
                                int dbScore = reader.GetInt32(1);

                                ranking.AppendLine($"랭킹 {rank}: {dbUserName} - {dbScore}");

                                rank++;
                            }
                            
                            // 새로운 XAML 페이지를 열고 순위 정보를 전달
                            RankingPage rankingPage = new RankingPage();
                            rankingPage.RankingText = ranking.ToString();
                            rankingPage.Show();
                            this.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("에러 메세지: " + ex.Message);
            }
        }
    }
}



