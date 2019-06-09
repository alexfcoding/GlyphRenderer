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

namespace GlyphRenderer
{
    public delegate DrawingContext DrawDelegate(DrawingContext drawingContext, BitmapSource image, string TextToDraw);
    
    public partial class MainWindow : Window
    {      

        List<String> FileExtensions = new List<string>();
        public DrawDelegate DrawWithSelectedAlgorithms;
        
        public MainWindow()
        {
            InitializeComponent();
                        
            watchFiles();

            FileExtensions.Add("*.png");
            FileExtensions.Add("*.jpg");

            FillListBox(listSource, @"prepare", FileExtensions);
            FillListBox(listProcessed, @"output", FileExtensions);

            if (listSource.Items[0] != null)
                listSource.SelectedIndex = 0;

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
                if (DrawWithSelectedAlgorithms != null)
                    CsharpAlgorithms();
                else
                    MessageBox.Show("Please select drawing algorithm");
           
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

        private void CsharpAlgorithms()
        {
            BitmapImage bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.CacheOption = BitmapCacheOption.OnLoad;
            bitImage.UriSource = new Uri(@"prepare\" + listSource.SelectedItem.ToString(), UriKind.Relative);
            bitImage.EndInit();

            var bmptmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);
           
            BitmapSource bitmapOutput;
            if (checkBoxDrawOnImage.IsChecked == true)
                bitmapOutput = ProcessImage(bitImage, true);
            else
                bitmapOutput = ProcessImage(bitImage, false);

            imageRenderer.Source = bitmapOutput;

            if (listSource.SelectedItem != null)
            {
                var encoder = new JpegBitmapEncoder(); // Or any other, e.g. PngBitmapEncoder for PNG.

                encoder.Frames.Add(BitmapFrame.Create(bitmapOutput));
                encoder.QualityLevel = 100; // Set quality level 1-100.

                Random rndFileName = new Random();

                using (var stream = new FileStream(@"output/" + rndFileName.Next(1, int.MaxValue).ToString() + "_" + listSource.SelectedItem.ToString(), FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }                        
        }
                
        public DrawingContext RenderGlyphsAlgorithm(DrawingContext drawingContext, BitmapSource image, string TextToDraw)
        {
            Random rndChar = new Random();

            for (int i = 0; i < image.PixelWidth; i += 50)
                for (int j = 0; j < image.PixelHeight; j += 50)
                {
                    GlyphRun gr = new GlyphRun(
                    new GlyphTypeface(new Uri(@"C:\Windows\Fonts\OldEgyptGlyphs.TTF")),
                    0,       
                    false,   
                    40,      
                    new ushort[] { (ushort)rndChar.Next(0, 200) }, 
                    new Point(i, j),          
                    new double[] { 50.0 },
                    null,    
                    null,    
                    null,    
                    null,    
                    null,    
                    null);   

                    SolidColorBrush textcolorBrush = new SolidColorBrush(GetPixel2(image, i, j));
                    drawingContext.DrawGlyphRun(textcolorBrush, gr);
                }

            return drawingContext;
        }

        public BitmapSource ProcessImage(BitmapSource image, bool DrawOnPicture)
        {
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

                string CharsToRender = "!?@#$%ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                                
                DrawWithSelectedAlgorithms(drawingContext, image, CharsToRender);                
            }
            
            Rect bounds = VisualTreeHelper.GetDescendantBounds(visual);

            var bitmap = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Default);

            bitmap.Render(visual);

            return bitmap;
        }

        public DrawingContext RenderCharsAlgorithm(DrawingContext drawingContext, BitmapSource image, string TextToDraw)
        {
            Random rndChar = new Random();

            for (int x = 0; x < image.PixelWidth; x += 9)
            {
                for (int y = 0; y < image.PixelHeight; y += 10)
                {
                    var text = new FormattedText(
                    TextToDraw[rndChar.Next(0, TextToDraw.Length)].ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    12,
                    Brushes.White);

                    text.SetForegroundBrush(new SolidColorBrush(GetPixel2(image, x, y)));
                    drawingContext.DrawText(text, new Point(x, y));
                }
            }

            return drawingContext;
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

            var resizedImage = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);

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

        private void CheckBoxDrawChars_Checked(object sender, RoutedEventArgs e)
        {
            DrawWithSelectedAlgorithms += RenderCharsAlgorithm;
        }

        private void CheckBoxDrawGlyphs_Checked(object sender, RoutedEventArgs e)
        {
            DrawWithSelectedAlgorithms += RenderGlyphsAlgorithm;
        }

        private void CheckBoxPython_Checked(object sender, RoutedEventArgs e)
        {
            checkBoxDrawChars.IsEnabled = false;
            checkBoxDrawGlyphs.IsEnabled = false;
        }

        private void CheckBoxPython_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBoxDrawChars.IsEnabled = true;
            checkBoxDrawGlyphs.IsEnabled = true;
        }
    }
}
