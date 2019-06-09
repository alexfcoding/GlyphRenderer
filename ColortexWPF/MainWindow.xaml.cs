using System;
using System.Collections.Generic;
using System.IO;
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
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace ColortexWPF
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {      

        List<String> FileExtensions = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            
            watchFiles();

            FileExtensions.Add("*.png");
            FileExtensions.Add("*.jpg");

            FillListBox(listSource, @"prepare", FileExtensions);
            FillListBox(listProcessed, @"output", FileExtensions);

            pythonPath.Text = LoadConfig(pythonPath)[0];
        }

        private void watchFiles()
        {
            FileSystemWatcher watcherInput = new FileSystemWatcher();
            
            watcherInput.Path = @"prepare";
            watcherInput.NotifyFilter = NotifyFilters.LastWrite;
            watcherInput.Filter = "*.*";
            watcherInput.Changed += new FileSystemEventHandler(OnNewFilesFound);
            watcherInput.EnableRaisingEvents = true;

            FileSystemWatcher watcherOutput = new FileSystemWatcher();
            
            watcherOutput.Path = @"output";
            watcherOutput.NotifyFilter = NotifyFilters.LastWrite;
            watcherOutput.Filter = "*.*";
            watcherOutput.Changed += new FileSystemEventHandler(OnNewFilesFound);
            watcherOutput.EnableRaisingEvents = true;
        }

        public void OnNewFilesFound(object source, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => refreshList()));
        }

        public void refreshList()
        {
            listSource.Items.Clear();
            FillListBox(listSource, @"prepare", FileExtensions);

            listProcessed.Items.Clear();
            FillListBox(listProcessed, @"output", FileExtensions);
        }

        private string[] LoadConfig(TextBox pythonPath)
        {
            string path = @"config.txt";

            string[] configLines;
            var list = new List<string>();
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }
            configLines = list.ToArray();
            return configLines;
        }

        private void FillListBox(ListBox inputListbox, string Folder, List<string> FileExtensions)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Folder);

            for (int i = 0; i < FileExtensions.Count; i++)
            {
                FileInfo[] Files = dirInfo.GetFiles(FileExtensions[i]);

                foreach (FileInfo file in Files)
                {
                    inputListbox.Items.Add(file.Name);
                }
            }
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxPython.IsChecked == true)
                PythonAlgorithm(@"main.py");
            else
                CsharpAlgorithm1();
        }

        private void PythonAlgorithm(string pythonScript)
        {
            string pyPath = pythonPath.Text;
            string pyScript = pythonScript;

            System.IO.DirectoryInfo di = new DirectoryInfo("input");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            if (listSource.SelectedItem != null)
            {
                var encoder = new JpegBitmapEncoder(); // Or any other, e.g. PngBitmapEncoder for PNG.
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageRenderer.Source));
                encoder.QualityLevel = 100; // Set quality level 1-100.

                using (var stream = new FileStream(@"input/" + listSource.SelectedItem.ToString(), FileMode.Create))
                {
                    encoder.Save(stream);
                }

                Thread.Sleep(200);
                ExecutePython(pyPath, pyScript);
            }
        }

        private void CsharpAlgorithm1()
        {                        
            BitmapImage bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.CacheOption = BitmapCacheOption.OnLoad;
            bitImage.UriSource = new Uri(@"prepare\" + listSource.SelectedItem.ToString(), UriKind.Relative);
            bitImage.EndInit();            

            WriteableBitmap writeableBmp = new WriteableBitmap(bitImage);

            int width = 300;
            int height = 300;
                       
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            // Create an array of pixels to contain pixel color values
            uint[] pixels = new uint[width * height];

            int red;
            int green;
            int blue;
            int alpha;

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    int i = width * y + x;

                    red = 0;
                    green = 255 * y / height;
                    blue = 255 * (width - x) / width;
                    alpha = 255;
                    pixels[i] = (uint)((blue << 24) + (green << 16) + (red << 8) + alpha);
                    
                }
            }
            
            bitmap.WritePixels(new Int32Rect(0, 0, 300, 300), pixels, width * 4, 0);
                       
            var visual = new DrawingVisual();
            
            Point p1 = new Point(0, 0);

            using (var r = visual.RenderOpen())
            {
                r.DrawImage(bitImage, new Rect(0, 0, bitImage.PixelWidth, bitImage.PixelHeight));

                for (int i = 0; i < bitImage.PixelWidth; i += 25)
                    for (int j = 0; j < bitImage.PixelHeight; j += 25)
                    {
                        GlyphRun gr = new GlyphRun(
                        new GlyphTypeface(new Uri(@"C:\Windows\Fonts\BKANT.TTF")),
                        0,       // Bi-directional nesting level
                        false,   // isSideways
                        25,      // pt size
                        new ushort[] { 23 },   // glyphIndices
                        new Point(i, j),           // baselineOrigin
                        new double[] { 80.0 },  // advanceWidths
                        null,    // glyphOffsets
                        null,    // characters
                        null,    // deviceFontName
                        null,    // clusterMap
                        null,    // caretStops
                        null);   // xmlLanguage
                                               
                        SolidColorBrush textcolorBrush = new SolidColorBrush(GetPixel(bitImage, i, j));
                        r.DrawGlyphRun(textcolorBrush, gr);
                    }
            }

            var target = new RenderTargetBitmap(bitImage.PixelWidth, bitImage.PixelHeight, bitImage.DpiX, bitImage.DpiY, PixelFormats.Pbgra32);

            target.Render(visual);  
            imageRenderer.Source = target;            
        }

        private void CsharpAlgorithm2()
        {
            BitmapImage bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.CacheOption = BitmapCacheOption.OnLoad;
            bitImage.UriSource = new Uri(@"prepare\" + listSource.SelectedItem.ToString(), UriKind.Relative);
            bitImage.EndInit();

            var bmptmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);
            string CharsToRender = "!?@#$%ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            
            BitmapSource bitmapOutput = RenderChars(bitImage, CharsToRender, true);
            imageRenderer.Source = bitmapOutput;

            if (listSource.SelectedItem != null)
            {
                var encoder = new JpegBitmapEncoder(); // Or any other, e.g. PngBitmapEncoder for PNG.
                                
                encoder.Frames.Add(BitmapFrame.Create(bitmapOutput));
                encoder.QualityLevel = 100; // Set quality level 1-100.

                using (var stream = new FileStream(@"output/" + listSource.SelectedItem.ToString(), FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }

        public BitmapSource RenderChars(BitmapSource image, string watermarkText, bool DrawOnPicture)
        {
            Random rndChar = new Random();
            
            var visual = new DrawingVisual();

            using (var drawingContext = visual.RenderOpen())
            {
                if (DrawOnPicture == true)
                    drawingContext.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                else
                {
                    var bmptmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);
                    var imgcreated = new TransformedBitmap(bmptmp, new ScaleTransform(image.PixelWidth, image.PixelHeight));
                    drawingContext.DrawImage(bmptmp, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                }
                       
                for (int x = 0; x < image.PixelWidth; x += 15)
                {
                    for (int y = 0; y < image.PixelHeight; y += 15)
                    {
                        var text = new FormattedText(
                        watermarkText[rndChar.Next(0, watermarkText.Length)].ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        15,
                        Brushes.White);

                        text.SetForegroundBrush(new SolidColorBrush(GetPixel2(image, x, y)));
                        drawingContext.DrawText(text, new Point(x, y));
                    }
                }
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(visual);

            var bitmap = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Default);

            bitmap.Render(visual);

            return bitmap;
        }
        
        public BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
        {
            BitmapImage bmp = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bmp.StreamSource = new MemoryStream(stream.ToArray()); //stream;
                bmp.EndInit();
                bmp.Freeze();
            }
            return bmp;
        }

        public Color GetPixel(BitmapSource bitmap, int x, int y)
        {
            Debug.Assert(bitmap != null);
            Debug.Assert(x >= 0);
            Debug.Assert(y >= 0);
            Debug.Assert(x < bitmap.Width);
            Debug.Assert(y < bitmap.Height);
            Debug.Assert(bitmap.Format.BitsPerPixel >= 24);

            CroppedBitmap cb = new CroppedBitmap(bitmap, new Int32Rect(x, y, 1, 1));
            byte[] pixel = new byte[bitmap.Format.BitsPerPixel / 8];
            cb.CopyPixels(pixel, bitmap.Format.BitsPerPixel / 8, 0);
            return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
        }

        public static Color GetPixel2(BitmapSource bitmap, int x, int y)
        {
            Color color;
            var bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            var bytes = new byte[bytesPerPixel];

            var rect = new Int32Rect(x, y, 1, 1);

            bitmap.CopyPixels(rect, bytes, bytesPerPixel, 0);

            if (bitmap.Format == PixelFormats.Bgra32)
            {
                color = Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
            }
            else if (bitmap.Format == PixelFormats.Bgr32)
            {
                color = Color.FromRgb(bytes[2], bytes[1], bytes[0]);
            }
            // handle other required formats
            else
            {
                color = Colors.Black;
            }

            return color;
        }

        private void ExecutePython(string pythonPath, string scriptName)
        {
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(pythonPath);
            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.RedirectStandardOutput = true;
            myProcessStartInfo.Arguments = "main.py";
            Process myProcess = new Process();
            myProcess.StartInfo = myProcessStartInfo;
            myProcess.Start();
        }

        private static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        private void ListSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {          
            if (listSource.SelectedItem != null)
            { 
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(@"prepare\" + listSource.SelectedItem.ToString(), UriKind.Relative);
                image.EndInit();
                imageRenderer.Source = image;
            }
            listProcessed.UnselectAll();
        }

        private void ListProcessed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listProcessed.SelectedItem != null)
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(@"output\" + listProcessed.SelectedItem.ToString(), UriKind.Relative);
                image.EndInit();
                imageRenderer.Source = image;
            }
            listSource.UnselectAll();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {            
            BitmapFrame bitmap = CreateResizedImage(imageRenderer.Source, (int)(imageRenderer.Source.Width / 1.2), (int)(imageRenderer.Source.Height / 1.2), 0);
                        
            imageRenderer.Source = bitmap;
        }
    }
}
