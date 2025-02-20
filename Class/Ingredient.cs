using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace test1
{
    public abstract class Ingredient
    {
        public Image Image { get; set; }
        public int Score { get; set; }
    }

    public class Bun : Ingredient
    {
        public Bun()
        {
            Image = new Image();
            Image.Source = new BitmapImage(new Uri("Ingredient_image/bun1.png", UriKind.Relative));
            Image.Height = 240;
        }
    }


    public class Patty : Ingredient
    {
        public Patty()
        {
            Image = new Image();
            Image.Source = new BitmapImage(new Uri("Ingredient_image/patty.png", UriKind.Relative));
            Image.Height = 60;
        }
    }

    public class Vegetable : Ingredient
    {
        public Vegetable()
        {
            Image = new Image();
            Image.Source = new BitmapImage(new Uri("Ingredient_image/vegetable.png", UriKind.Relative));
            Image.Height = 80;
        }
    }
    public class Tomato : Ingredient
    {
        public Tomato()
        {
            Image = new Image();
            Image.Source = new BitmapImage(new Uri("Ingredient_image/tomato.png", UriKind.Relative));
            Image.Height = 80;
        }
    }

    public class Shrimp : Ingredient
    {
        public Shrimp()
        {
            Image = new Image();
            Image.Source = new BitmapImage(new Uri("Ingredient_image/shrimp.png", UriKind.Relative));
            Image.Height = 120;
        }
    }

    public class Boom : Ingredient
    {
        public Boom()
        {
            Image = new Image();
            Image.Source = new BitmapImage(new Uri("Ingredient_image/boom.png", UriKind.Relative));

        }
    }


    public class IngredientFactory
    {
        private Random random = new Random();

        public Ingredient CreateRandomIngredient()
        {
            int type = random.Next(10); // 0~9 중 임의의 숫자를 생성

            switch (type)
            {
                case 0:
                case 1:
                    return new Bun(); //20%
                case 2:
                case 3:
                    return new Patty(); //10%
                case 4:  
                case 5:
                    return new Vegetable(); //20%
                case 6:
                case 7:
                    return new Tomato(); //20%
                case 8:
                    return new Shrimp(); //10%
                case 9: 
                    return new Boom(); //10%
                default:
                    return null;
            }
        }


    }
}
