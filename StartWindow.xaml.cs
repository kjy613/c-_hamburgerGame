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
using System.Windows.Shapes;
using static test1.UserManager;

namespace test1
{
    /// <summary>
    /// StartWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StartWindow : Window
    {
        
        public StartWindow()
        {
            InitializeComponent();
           
        }
       

        private void StartButton_Click_1(object sender, RoutedEventArgs e)
        {
            string connectionString = "Server=localhost;Database=gamedb;Uid=root;Pwd=rootpw;";
            UserManager userManager = new UserManager(connectionString);
            if (userManager.IsUserNameDuplicate(UserNameTextBox.Text))
            {
                MessageBox.Show($"{UserNameTextBox.Text}는 이미 등록된 닉네임입니다.");
            }
            else
            {
                User.UserName = UserNameTextBox.Text;
                ExplainWindow explainWindow = new ExplainWindow();   
                explainWindow.Show();
                this.Close();
            }
            
        }
    }
}
