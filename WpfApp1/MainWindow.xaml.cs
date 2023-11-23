using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System;
using System.ComponentModel.Design;
using System.Windows.Media.Media3D;
using System.IO;
using Newtonsoft.Json;
using DrawingPoint = System.Drawing.Point;
using System.Drawing;
using System.Globalization;


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public class CanvasInfo
    {
        public List<Paint> Paints { get; set; }
        public List<LineInfo> Lines { get; set; }
        public List<SquareInfo> Squares { get; set; }
        public string ImageFilePath { get; set; }
    }

    public class Paint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double size { get; set; }
        public BrushInfo Brush { get; set; }
        
    }

    public class BrushInfo
    {
        public List<GradientStopInfo> GradientStops { get; set; }
    }

    public class GradientStopInfo
    {
        public string Color { get; set; }
        public double Offset { get; set; }
    }

    public class LineInfo
    {
        public System.Windows.Point StartPoint { get; set; }
        public System.Windows.Point EndPoint { get; set; }
        public double Thickness { get; set; }
        public string Color { get; set; }
    }

    public class SquareInfo
    {
        public double X { get; set; } // X-coordinate of the top-left corner
        public double Y { get; set; } // Y-coordinate of the top-left corner
        public double Size { get; set; }
        public string Color { get; set; }
        public double Thickness { get; set; }
    }

    public partial class MainWindow : Window
    {


        private bool isDrawing;
        private bool isErasing;
        private bool isUploadImage = false;

        private int thickness = 1;
        private int spraydensity = 1;

        public List<string> ColorsPalette { get; } = new List<string> { "White", "LightGray", "Gray", "Black", "DarkRed", "Red",
                                                                        "Coral", "Orange", "Beige", "Yellow", "LightGreen", "Green", 
                                                                        "LightBlue", "Blue", "DarkBlue",  "Purple", "Plum", "Pink"};

        private string uploadImgFilePath;
        private string appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private readonly SolidColorBrush originalColor = new SolidColorBrush(Colors.LightGray); 
        private readonly SolidColorBrush selectedColor = new SolidColorBrush(Colors.Gray);
        private Button currentTool = null;
        private Line currentLine;
        private System.Windows.Point SquarestartPoint;
        private System.Windows.Shapes.Rectangle currentSquare;


        SolidColorBrush brush = new SolidColorBrush();



        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        //EVent handler for upload image from local file
        private void uploadImgOnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "All supported graphics|*.jpg;*.jpeg;*.png;*.bmp|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";

            dlg.RestoreDirectory = true;
            if(dlg.ShowDialog() == true)
            { 
                ImageViewer.Source = new BitmapImage(new Uri(dlg.FileName));
                uploadImgFilePath = dlg.FileName;
                isUploadImage = true;
            }

        }

        //Event handler for saving canvas drawings into JSON file
        private void saveCanvasOnClick(object sender, RoutedEventArgs e)
        {
            //save paint and image file path into JSON file
            try
            {
                var CanvasData = GetPaintingFromCanvas();

                string json = JsonConvert.SerializeObject(CanvasData);
                File.WriteAllText(appDirectory + "\\paints_saving.json", json);

            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"Failed to save drawings: {jsonEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"File error: {ioEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// When saving current drawings, we need to get all the drawings and brush informations and put them into JSON file
        /// All the information are in CanvasInfo class
        /// </summary>        
        private CanvasInfo GetPaintingFromCanvas()
        {
            List<Paint> paints = new List<Paint>();
            List< LineInfo> lines = new List<LineInfo>();
            List<SquareInfo> squares = new List<SquareInfo>();
            foreach (var child in DrawingCanvas.Children)
            {
                if (child is Ellipse ellipse)
                {
                    var p = new Paint
                    {
                        X = Canvas.GetLeft(ellipse),
                        Y = Canvas.GetTop(ellipse),
                        size = ellipse.Width, // Assuming width and height are same for circles
                        Brush = GetBrushInfoFromCanvas(ellipse.Fill)
                    };
                    paints.Add(p);
                }
                else if(child is Line line)
                {
                    var l = new LineInfo
                    {
                        StartPoint = new System.Windows.Point(line.X1, line.Y1),
                        EndPoint = new System.Windows.Point(line.X2, line.Y2),
                        Thickness = line.StrokeThickness,
                        Color = line.Stroke.ToString()
                    };
                    lines.Add(l);
                }
                else if(child is System.Windows.Shapes.Rectangle rectangle)
                {
                    var rect = new SquareInfo
                    {
                        X = Canvas.GetLeft(rectangle),
                        Y = Canvas.GetTop(rectangle),
                        Size = rectangle.Width,
                        Thickness = rectangle.StrokeThickness,
                        Color = rectangle.Stroke.ToString()
                    };
                    squares.Add(rect);
                }
                    
            }
            var CanvasData = new CanvasInfo
            {
                Paints = paints,
                Lines = lines,
                Squares = squares,
                ImageFilePath = uploadImgFilePath
            };
            return CanvasData;
        }

        private BrushInfo GetBrushInfoFromCanvas(Brush brush)
        {
            var brushInfo = new BrushInfo();
            if (brush is RadialGradientBrush radialGradientBrush)
            {
                brushInfo.GradientStops = radialGradientBrush.GradientStops
                    .Select(gs => new GradientStopInfo { Color = gs.Color.ToString(), Offset = gs.Offset })
                    .ToList();
            }

            return brushInfo;
        }

        //Event handler for loading previous works to canvas
        private void loadPaintingOnClick(object sender, RoutedEventArgs e)
        {
            string filePath = appDirectory + "\\paints_saving.json";
            Debug.WriteLine(filePath);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var canvasData = JsonConvert.DeserializeObject<CanvasInfo>(json);
                    if (canvasData != null)
                    {
                        if(!string.IsNullOrEmpty(canvasData.ImageFilePath) && File.Exists(canvasData.ImageFilePath))
                        {
                            //Load Image if path are valid
                            ImageViewer.Source = new BitmapImage(new Uri(canvasData.ImageFilePath));
                            uploadImgFilePath = canvasData.ImageFilePath;
                            isUploadImage = true;

                            RecreatePaintings(canvasData);
                        }
                    }
                    
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}");
                    Debug.WriteLine("Cannot load JSON file");
                }
            }
        }

        /// <summary>
        /// Recreate the previous drawings on the canvas, enable user to continue editting
        /// </summary>
        private void RecreatePaintings(CanvasInfo canvasData)
        {
            
            foreach (var painting in canvasData.Paints)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = painting.size,
                    Height = painting.size,
                    Fill = loadBrushInfoFromJSON(painting.Brush)
                };

                Canvas.SetLeft(ellipse, painting.X);
                Canvas.SetTop(ellipse, painting.Y);

                DrawingCanvas.Children.Add(ellipse);
            }
            foreach(var line in canvasData.Lines)
            {

                SolidColorBrush lineBrush = new SolidColorBrush(brush.Color);
                Line l = new Line
                {
                    Stroke = lineBrush,
                    StrokeThickness = line.Thickness,
                    X1 = line.StartPoint.X,
                    Y1 = line.StartPoint.Y,
                    X2 = line.EndPoint.X,
                    Y2 = line.EndPoint.Y

                };
                DrawingCanvas.Children.Add(l);
            }
            foreach(var squareInfo in canvasData.Squares)
            {
                SolidColorBrush lineBrush = new SolidColorBrush(brush.Color);
                System.Windows.Shapes.Rectangle squares = new System.Windows.Shapes.Rectangle
                {
                    Stroke = lineBrush,
                    StrokeThickness = squareInfo.Thickness,
                    Width = squareInfo.Size,
                    Height = squareInfo.Size
                };
                Canvas.SetLeft(squares, squareInfo.X);
                Canvas.SetTop(squares, squareInfo.Y);
                DrawingCanvas.Children.Add(squares);
            }
        }

        /// <summary>
        /// Loading drawing Info. from JSON file
        /// </summary>
        private Brush loadBrushInfoFromJSON(BrushInfo brushInfo)
        {
            if (brushInfo.GradientStops != null && brushInfo.GradientStops.Any())
            {

                var radialBrush = new RadialGradientBrush
                {
                    GradientOrigin = new System.Windows.Point(0.5, 0.5),
                    Center = new System.Windows.Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5
                };

                foreach (var stop in brushInfo.GradientStops)
                {
                    radialBrush.GradientStops.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(stop.Color), stop.Offset));
                }

                return radialBrush;
            }
            
            return Brushes.Transparent;
        }


        //event handler for downloading canvas image & drawings to local image file
        private void downloadImgOnClick(object sender, RoutedEventArgs e)
        {
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                saveCanvasToLocalImageFile(saveFileDialog.FileName);
            }
                
        }
        // Using bitmap to save both original image and canvas drawings
        private void saveCanvasToLocalImageFile(string filename)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)DrawingCanvas.ActualWidth, (int)DrawingCanvas.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            bitmap.Render(ImageViewer); //save both drawings and original image
            bitmap.Render(DrawingCanvas);
            
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            

            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                BitmapEncoder encoder;
                if (System.IO.Path.GetExtension(filename).Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    encoder = new JpegBitmapEncoder();
                }
                else
                {
                    encoder = new PngBitmapEncoder();
                }
                encoder.Frames.Add(frame);
                encoder.Save(fileStream);
            }

        }



        // Mouse Events for drawing and erasing
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentTool != null && e.ButtonState == MouseButtonState.Pressed)
            {
                if(currentTool.Tag.ToString() == "Spray")
                {
                    isDrawing = true;
                }
                else if(currentTool.Tag.ToString() == "Line")
                {
                    SolidColorBrush lineBrush = new SolidColorBrush(brush.Color); // CurrentLineColor is the selected color

                    currentLine = new Line
                    {
                        Stroke = lineBrush,
                        StrokeThickness = thickness,
                        X1 = e.GetPosition(DrawingCanvas).X,
                        Y1 = e.GetPosition(DrawingCanvas).Y
                    };
                    DrawingCanvas.Children.Add(currentLine);
                    isDrawing = true;
                }
                else if(currentTool.Tag.ToString() == "Square")
                {
                    
                    SolidColorBrush lineBrush = new SolidColorBrush(brush.Color); // CurrentLineColor is the selected color
                    SquarestartPoint = e.GetPosition(DrawingCanvas);
                    currentSquare = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = lineBrush,
                        StrokeThickness = thickness
                    };
                    Canvas.SetLeft(currentSquare, SquarestartPoint.X);
                    Canvas.SetTop(currentSquare, SquarestartPoint.Y);
                    DrawingCanvas.Children.Add(currentSquare);
                    isDrawing = true;
                }
                else if(currentTool.Tag.ToString() == "Eraser")
                {
                    isErasing = true;
                }
            }

        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (isDrawing && isUploadImage)
            {
                if(currentTool.Tag.ToString() == "Spray")
                {
                    System.Windows.Point currentPoint = e.GetPosition(DrawingCanvas);
                    sprayPainting(currentPoint);
                }
                else if(currentTool.Tag.ToString() == "Line")
                {

                    currentLine.X2 = e.GetPosition(DrawingCanvas).X;
                    currentLine.Y2 = e.GetPosition(DrawingCanvas).Y;
                }
                else if(currentTool.Tag.ToString() == "Square")
                {
                    System.Windows.Point currentPoint = e.GetPosition(DrawingCanvas);
                    double size = Math.Max(Math.Abs(currentPoint.X - SquarestartPoint.X), Math.Abs(currentPoint.Y - SquarestartPoint.Y));

                    currentSquare.Width = size;
                    currentSquare.Height = size;

                    if (currentPoint.X < SquarestartPoint.X)
                        Canvas.SetLeft(currentSquare, SquarestartPoint.X - size);
                    if (currentPoint.Y < SquarestartPoint.Y)
                        Canvas.SetTop(currentSquare, SquarestartPoint.Y - size);
                }


            }
            /// <summary>
            /// Erase the drawings on Canvas. Changing erasing area by modifying eraserRadius
            /// </summary>
            if (isErasing)
            {
                double eraserRadius = thickness * 10;
                System.Windows.Point mousePosition = e.GetPosition(DrawingCanvas);
                EllipseGeometry eraserArea = new EllipseGeometry(mousePosition, eraserRadius, eraserRadius);
                VisualTreeHelper.HitTest(DrawingCanvas, null, new HitTestResultCallback(EraserHitTestResult), new GeometryHitTestParameters(eraserArea));
                
            }

        }

        // Implement spray painting by RadialGradientBrush
        private void sprayPainting(System.Windows.Point currentPoint)
        {
            
            int numberOfDots = Convert.ToInt32(Math.Pow(2, spraydensity) * 10); //control density
            Random random = new Random();

            for (int i = 0; i < numberOfDots; i++)
            {
                //Settint spray brush
                RadialGradientBrush sprayBrush = new RadialGradientBrush
                {
                    GradientOrigin = new System.Windows.Point(0.5, 0.5),
                    Center = new System.Windows.Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5
                };

                sprayBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                sprayBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(102, brush.Color.R, brush.Color.G, brush.Color.B), 0.3));
                sprayBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(70, brush.Color.R, brush.Color.G, brush.Color.B), 0.1));
                //sprayBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, brush.Color.R, brush.Color.G, brush.Color.B), 0.3));


                //setting the size of ellipse
                double dotSize = 10;
                double opacity = 0.2; // the lower the more transparent
                double offsetX = (random.NextDouble() - 0.5) * 10; //the range of the spray
                double offsetY = (random.NextDouble() - 0.5) * 10;

                Ellipse ellipse = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = sprayBrush,
                    Opacity = opacity
                };


                Canvas.SetLeft(ellipse, currentPoint.X + offsetX);
                Canvas.SetTop(ellipse, currentPoint.Y + offsetY);

                DrawingCanvas.Children.Add(ellipse);
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
            isErasing = false;
        }

        //Remove the drawing by mouse event
        private HitTestResultBehavior EraserHitTestResult(HitTestResult result)
        {
            if (result.VisualHit is Shape shape)
            {
                DrawingCanvas.Children.Remove(shape);
            }
            return HitTestResultBehavior.Continue;
        }

        

        // Event handler for clearing all drawings on canvas
        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
        }

        // Event handler for Painting and Erase Tools
        private void SprayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                changeToolType("Spray", clickedButton);
            }
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                changeToolType("Line", clickedButton);
            }
        }

        private void SquareButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                changeToolType("Square", clickedButton);
            }
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                changeToolType("Eraser", clickedButton);
            }
                
        }

        //Event Hanler for Thiackness Slider's value
        private void GetValueThicknessSlider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int thicknessValue = (int)ThicknessSlider.Value;
            thickness = thicknessValue;

        }
        //Event Hanler for Density Slider's value
        private void GetValueDensitySlider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int density = (int)DensitySlider.Value;
            spraydensity = density;

        }

        // Modify currently selected Tool, if click twice it will unselect the tool
        private void changeToolType(string curToolType, Button clickedButton)
        {

            if (currentTool != clickedButton)
            {
                clickedButton.Background = selectedColor;
                if (currentTool != null)
                {
                    currentTool.Background = originalColor;
                }
                currentTool = clickedButton;
            }
            else
            {
                clickedButton.Background = originalColor;
                currentTool = null;
            }
        }

        // Event handler for selecting colors
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string selectedColorName = button.Tag.ToString();
                brush.Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(selectedColorName);

            }
        }
    }


}