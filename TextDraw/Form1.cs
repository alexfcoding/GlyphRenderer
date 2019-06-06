using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Svg;


namespace TextDraw
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnRunPython_Click(object sender, EventArgs e)
        {
            string pyPath = @"C:\Users\Александр\AppData\Local\Programs\Python\Python37\python.exe";
            string pyScript = @"C:\Users\Александр\source\Python\recursion_tree-master\RecursionTree\main.py";

            ExecutePython(pyPath, pyScript);
        }

        private void ExecutePython (string pythonPath, string scriptName)
        {
            Process p = new Process();
            p.StartInfo.FileName = pythonPath;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.Arguments = scriptName;                    
            p.Start();
        }

        private void RenderSVG (string filePath)
        {
            var svgDoc = SvgDocument.Open(filePath);

            PictureSVGRender.Image = svgDoc.Draw();
        }
        private void BtnDrawSVG_Click(object sender, EventArgs e)
        {
            string svgPath = @"C:\Users\Александр\source\Python\recursion_tree-master\RecursionTree\random_tree_001.svg";

            RenderSVG(svgPath);
        }

        private void BtnConvert_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            DrawTextOnPic(pictureBox1.Image, watch);
        }

        private void DrawTextOnPic(Image Picture, Stopwatch watch)
        {
            char[] charOut = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

            Bitmap sourcePic = new Bitmap(Picture);
            
            Graphics clearGraphics = Graphics.FromImage(Picture);
            Brush clearBrush = new SolidBrush(Color.Black);

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
                                Color clr = sourcePic.GetPixel(i, j);
                                Brush brush = new SolidBrush(clr);
                                graphics.DrawString(charOut[randomCharIndex].ToString(), font, brush, i, j);
                            }

                        }
                    }
                    pictureBox1.Refresh();
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            listBox1.Items.Add("Execution time:");
            listBox1.Items.Add(elapsedMs + " ms");
            listBox1.Items.Add("=========================");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Random rndNum = new Random();
            int RandomNum = rndNum.Next(0, int.MaxValue);

            pictureBox1.Image.Save($"pic{RandomNum}.jpg");
        }
        
    }
}
//randomNum = rndNum.Next(0, 255);
//byte red = Convert.ToByte(randomNum);
//randomNum = rndNum.Next(0, 255);
//byte green = Convert.ToByte(randomNum);
//randomNum = rndNum.Next(0, 255);
//byte blue = Convert.ToByte(randomNum); ;