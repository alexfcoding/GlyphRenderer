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
using System.Net;
using System.Net.Sockets;
using System.Drawing;

namespace GlyphRenderer
{
    public delegate DrawingContext DrawDelegate(DrawingContext drawingContext, BitmapSource image, string TextToDraw);
    
    public partial class MainWindow : Window
    {
        string globalSelector;

        List<String> FileExtensions = new List<string>();
        DrawDelegate ApplySelectedAlgorithms;
        
        public MainWindow()
        {
            InitializeComponent();

            pictureBox.Image = System.Drawing.Image.FromFile("c:\\1.jpg");
            pictureBox.Visible = true;

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

        private void FillListBox(ListBox inputListbox, string Folder, List<string> fileExtensions)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Folder);

            for (int i = 0; i < fileExtensions.Count; i++)
            {
                FileInfo[] Files = dirInfo.GetFiles(fileExtensions[i]);

                foreach (FileInfo file in Files)
                {
                    inputListbox.Items.Add(file.Name);
                }
            }
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            var watch = Stopwatch.StartNew();
            DrawTextOnPic(pictureBox.Image, watch);
            //StartConverting();
        }

        private void DrawTextOnPic(System.Drawing.Image Picture, Stopwatch watch)
        {
            char[] charOut = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

            Bitmap sourcePic = new Bitmap(Picture);

            Graphics clearGraphics = Graphics.FromImage(Picture);
            System.Drawing.Brush clearBrush = new SolidBrush(System.Drawing.Color.Black);

            clearGraphics.FillRectangle(clearBrush, new System.Drawing.Rectangle(0, 0, Picture.Width, Picture.Height));

            Random rndNum = new Random();

            using (Graphics graphics = Graphics.FromImage(Picture))
            {
                using (Font font = new Font("Consolas", 15))
                {
                    for (int i = 0; i < Picture.Width; i += 12)
                    {
                        for (int j = 0; j < Picture.Height; j += 12)
                        {
                            int randomCharIndex = rndNum.Next(0, charOut.Length);

                            //if (sourcePic.GetPixel(i,j).R < 255)
                            {
                                System.Drawing.Color clr = sourcePic.GetPixel(i, j);
                                System.Drawing.Brush brush = new SolidBrush(clr);
                                graphics.DrawString(charOut[randomCharIndex].ToString(), font, brush, i, j);
                            }

                        }
                    }

                    pictureBox.Refresh();
                    pictureBox.Visible = true;
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
        }

        private void StartConverting()
        {
            if (checkBoxPython.IsChecked == true)
                PythonAlgorithm(@"main.py");
            else
               if (ApplySelectedAlgorithms != null)
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
               
            bitImage.UriSource = new Uri(@"prepare\" + globalSelector, UriKind.Relative);
            bitImage.EndInit();

            var bmptmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);
           
            BitmapSource bitmapOutput;
            if (checkBoxDrawOnImage.IsChecked == true)
                bitmapOutput = ProcessImage(bitImage, true);
            else
                bitmapOutput = ProcessImage(bitImage, false);

            imageRenderer.Source = bitmapOutput;

            ExportFile(bitmapOutput);
        }
                
        public DrawingContext RenderGlyphsAlgorithm(DrawingContext drawingContext, BitmapSource image, string textToDraw)
        {
            Random rndChar = new Random();
            int fontSize = (int)sliderFontSize.Value;
            int fontResolution = (int)sliderFontResolution.Value;

            for (int i = 0; i < image.PixelWidth; i += fontResolution)
                for (int j = 0; j < image.PixelHeight; j += fontResolution)
                {
                    GlyphRun gr = new GlyphRun(
                    new GlyphTypeface(new Uri(@"C:\Windows\Fonts\OldEgyptGlyphs.TTF")),
                    0,       
                    false,   
                    fontSize,      
                    new ushort[] { (ushort)rndChar.Next(0, 200) }, 
                    new System.Windows.Point(i, j),          
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

        public BitmapSource ProcessImage(BitmapSource image, bool drawOnPicture)
        {
            var visual = new DrawingVisual();

            using (var drawingContext = visual.RenderOpen())
            {
                if (drawOnPicture == true)
                    drawingContext.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                else
                {
                    var bmptmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);
                    var imgcreated = new TransformedBitmap(bmptmp, new ScaleTransform(image.PixelWidth, image.PixelHeight));
                    drawingContext.DrawImage(bmptmp, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                }

                string CharsToRender = "01";
                                
                ApplySelectedAlgorithms(drawingContext, image, CharsToRender);                
            }
            
            Rect bounds = VisualTreeHelper.GetDescendantBounds(visual);

            var bitmap = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Default);

            bitmap.Render(visual);

            return bitmap;
        }

        public DrawingContext RenderCharsAlgorithm(DrawingContext drawingContext, BitmapSource image, string textToDraw)
        {
            Random rndChar = new Random();
            int fontSize = (int)sliderFontSizeChars.Value;
            int fontResolution = (int)sliderFontResolutionChars.Value;

            for (int x = 0; x < image.PixelWidth; x += fontResolution)
            {
                for (int y = 0; y < image.PixelHeight; y += fontResolution)
                {
                    var text = new FormattedText(
                    textToDraw[rndChar.Next(0, textToDraw.Length)].ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    fontSize,
                    System.Windows.Media.Brushes.White);

                    text.SetForegroundBrush(new SolidColorBrush(GetPixel2(image, x, y)));
                    drawingContext.DrawText(text, new System.Windows.Point(x, y));
                }
            }

            return drawingContext;
        }

        public void ExportFile (BitmapSource imageToExport)
        {
            var encoder = new JpegBitmapEncoder(); // Or any other, e.g. PngBitmapEncoder for PNG.

            encoder.Frames.Add(BitmapFrame.Create(imageToExport));
            encoder.QualityLevel = 100; // Set quality level 1-100.

            Random rndFileName = new Random();

            using (var stream = new FileStream(@"output/" + rndFileName.Next(1, int.MaxValue).ToString() + "_" + globalSelector, FileMode.Create))
            {
                encoder.Save(stream);
            }
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

        public System.Windows.Media.Color GetPixel(BitmapSource bitmap, int x, int y)
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
            return System.Windows.Media.Color.FromRgb(pixel[2], pixel[1], pixel[0]);
        }

        public static System.Windows.Media.Color GetPixel2(BitmapSource bitmap, int x, int y)
        {
            System.Windows.Media.Color color;
            var bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            var bytes = new byte[bytesPerPixel];
            var rect = new Int32Rect(x, y, 1, 1);

            bitmap.CopyPixels(rect, bytes, bytesPerPixel, 0);

            if (bitmap.Format == PixelFormats.Bgra32)
            {
                color = System.Windows.Media.Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
            }
            else if (bitmap.Format == PixelFormats.Bgr32)
            {
                color = System.Windows.Media.Color.FromRgb(bytes[2], bytes[1], bytes[0]);
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
                globalSelector = listSource.SelectedItem.ToString();

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
            ApplySelectedAlgorithms += RenderCharsAlgorithm;
        }

        private void CheckBoxDrawChars_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplySelectedAlgorithms -= RenderCharsAlgorithm;
        }

        private void CheckBoxDrawGlyphs_Checked(object sender, RoutedEventArgs e)
        {
            ApplySelectedAlgorithms += RenderGlyphsAlgorithm;
        }

        private void CheckBoxDrawGlyphs_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplySelectedAlgorithms -= RenderGlyphsAlgorithm;
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

        private void SliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelFontSize.Content = $"Glyph size: { sliderFontSize.Value.ToString()}";
        }

        private void SliderFontResolution_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelFontResolution.Content = $"Glyph interval: {sliderFontResolution.Value.ToString()}";
        }

        private void SliderFontSizeChars_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelFontSizeChars.Content = $"Font size: { sliderFontSizeChars.Value.ToString()}";
        }

        private void SliderFontResolutionChars_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelFontResolutionChars.Content = $"Font interval: {sliderFontResolutionChars.Value.ToString()}";
        }

        private void SliderFontSize_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StartConverting();
        }

        private void SliderFontResolution_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StartConverting();
        }

        private void AppExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void btnConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            cpuCounter.NextValue();
            Thread.Sleep(100);
                        
            int port = 5555;
            string address = "127.0.0.1";

            try
            {
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к удаленному хосту
                socket.Connect(ipPoint);
                MessageBox.Show(cpuCounter.NextValue().ToString() + "%");
                listServerCommunication.Items.Add("Sending cpu load to server...");
                string message = Math.Round(cpuCounter.NextValue()) + "%";
                byte[] data = Encoding.UTF8.GetBytes(message);
                socket.Send(data);
                                
                data = new byte[256]; // буфер для ответа
                StringBuilder builder = new StringBuilder();
                int bytes = 0; // количество полученных байт

                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);

                listServerCommunication.Items.Add("Server answer: " + builder.ToString());
                                
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
