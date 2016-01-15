using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.Windows.Shapes.Path;

namespace Vot2015_Viewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox.Text.Length != 0)
                LoadImagesButton.IsEnabled = true;
            else
                LoadImagesButton.IsEnabled = false;
        }

        class GroundTruthBox
        {
            public Point UpLeft = new Point();
            public Point UpRight = new Point();
            public Point DownLeft = new Point();
            public Point DownRight = new Point();
        }

        private readonly IList<GroundTruthBox> _groundTruthBoxs = new List<GroundTruthBox>();
        private string[] _imageFiles;
        private async Task ParseGroundTruthTxt(string path)
        {
            _groundTruthBoxs.Clear();
            using (StreamReader reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    
                    string[] points = line.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries);

                    GroundTruthBox groundTruthBox = new GroundTruthBox();
                    
                    groundTruthBox.DownLeft.X = double.Parse(points[0]);
                    groundTruthBox.DownLeft.Y = double.Parse(points[1]);
                    groundTruthBox.DownRight.X = double.Parse(points[2]);
                    groundTruthBox.DownRight.Y = double.Parse(points[3]);
                    groundTruthBox.UpRight.X = double.Parse(points[4]);
                    groundTruthBox.UpRight.Y = double.Parse(points[5]);
                    groundTruthBox.UpLeft.X = double.Parse(points[6]);
                    groundTruthBox.UpLeft.Y = double.Parse(points[7]);

                    _groundTruthBoxs.Add(groundTruthBox);
                }
            }
        }
        
        private async void LoadImageAndDrawBoundingBox(int index)
        {
            string path = _imageFiles[index];
            var image = await Task.Run(() =>
            {
                try
                {
                    BitmapImage bitmapImage = new BitmapImage(new Uri(path));
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
                catch (Exception)
                {
                    return null;
                }
            });

            Canvas.Width = image.PixelWidth;
            Canvas.Height = image.PixelHeight;

            Canvas.Background = new ImageBrush(image);
            Canvas.Children.Clear();

            GroundTruthBox groundTruthBox = _groundTruthBoxs[index];

            Path boxPath = new Path()
            {
                Data = new LineGeometry(groundTruthBox.UpLeft, groundTruthBox.UpRight),
                Stroke = Brushes.Red,
                StrokeThickness = 4
            };
            Canvas.Children.Add(boxPath);
            boxPath = new Path()
            {
                Data = new LineGeometry(groundTruthBox.UpRight, groundTruthBox.DownRight),
                Stroke = Brushes.Red,
                StrokeThickness = 4
            };
            Canvas.Children.Add(boxPath);
            boxPath = new Path()
            {
                Data = new LineGeometry(groundTruthBox.DownRight, groundTruthBox.DownLeft),
                Stroke = Brushes.Red,
                StrokeThickness = 4
            };
            Canvas.Children.Add(boxPath);
            boxPath = new Path()
            {
                Data = new LineGeometry(groundTruthBox.DownLeft, groundTruthBox.UpLeft),
                Stroke = Brushes.Red,
                StrokeThickness = 4
            };
            Canvas.Children.Add(boxPath);
        }

        private void DeterminePrevNextButtonShouldBeEnable(int index)
        {
            if (index == 0)
                PrevButton.IsEnabled = false;
            else
            {
                PrevButton.IsEnabled = true;
            }
            if (index + 1 == _groundTruthBoxs.Count)
                NextButton.IsEnabled = false;
            else
            {
                NextButton.IsEnabled = true;
            }
        }


        private int _index;

        private void UiUpdateOnLoadNewImage()
        {
            DeterminePrevNextButtonShouldBeEnable(_index);
            LoadImageAndDrawBoundingBox(_index);
            IndexLabel.Content = $"{_index + 1}/{_groundTruthBoxs.Count}";
        }

        private async void LoadImagesButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PathTextBox.Text.StartsWith("\"") && PathTextBox.Text.EndsWith("\""))
            {
                PathTextBox.Text = PathTextBox.Text.Substring(1, PathTextBox.Text.Length - 2);
            }
            string path = PathTextBox.Text;
            
            if (!File.Exists(path))
                return;

            await ParseGroundTruthTxt(PathTextBox.Text);
            string s =Directory.GetParent(path).FullName;
            if (_groundTruthBoxs.Count != 0)
            {
                _imageFiles = Directory.GetFiles(Directory.GetParent(path).FullName, "*.jpg");
                _index = 0;
                UiUpdateOnLoadNewImage();
            }
        }

        private void PrevButton_OnClick(object sender, RoutedEventArgs e)
        {
            _index--;
            UiUpdateOnLoadNewImage();
        }

        private void NextButton_OnClick(object sender, RoutedEventArgs e)
        {
            _index++;
            UiUpdateOnLoadNewImage();
        }

        private void BrowseGroundTruthButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Ground Truth File(groundtruth.txt)|groundtruth.txt" };

            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue == false || result.Value == false)
                return;

            PathTextBox.Text = openFileDialog.FileName;

            LoadImagesButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                if (NextButton.IsEnabled)
                    NextButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            else if (e.Key == Key.Left)
            {
                if (PrevButton.IsEnabled)
                    PrevButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            e.Handled = true;
        }
    }
}
