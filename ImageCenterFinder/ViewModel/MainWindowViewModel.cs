using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageCenterFinder.ViewModel
{
    class MainWindowViewModel : BindableBase, IMouseCaptureProxy
    {
        public CommandBase LoadImageCommand { get; private set; }
        public CommandBase SaveImageCommand { get; private set; }

        private WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { SetProperty(ref _image, value); }
        }

        private double _imageTemplateWidth = 400;
        public double ImageTemplateWidth
        {
            get { return _imageTemplateWidth; }
            set { SetProperty(ref _imageTemplateWidth, value); }
        }

        private double _imageTemplateHeight = 400;
        public double ImageTemplateHeight
        {
            get { return _imageTemplateHeight; }
            set { SetProperty(ref _imageTemplateHeight, value); }
        }

        private string _xCenter;
        public string XCenter
        {
            get { return _xCenter; }
            set { SetProperty(ref _xCenter, value); }
        }

        private string _yCenter;
        public string YCenter
        {
            get { return _yCenter; }
            set { SetProperty(ref _yCenter, value); }
        }

        private string _mouseX;
        public string MouseX
        {
            get { return _mouseX; }
            set { SetProperty(ref _mouseX, value); }
        }

        private string _mouseY;
        public string MouseY
        {
            get { return _mouseY; }
            set { SetProperty(ref _mouseY, value); }
        }

        private double _distance = 0;
        public double Distance
        {
            get { return _distance; }
            set { SetProperty(ref _distance, value); }
        }

        private bool isCenterComputed = false;

        public MainWindowViewModel()
        {
            LoadImageCommand = new CommandBase(LoadImage);
            SaveImageCommand = new CommandBase(SaveImage, DoesImageExist);
        }

        private void LoadImage(object sender)
        {
            OpenFileDialog openFile = new OpenFileDialog();

            openFile.DefaultExt = ".png";

            openFile.Filter = "Pictures (*.jpg;*.gif;*.png)|*.jpg;*.gif;*.png";

            if(openFile.ShowDialog() == true)
            {
                ComputeImageCenter(new BitmapImage(new Uri(openFile.FileName, UriKind.Absolute)));
            }
        }

        private void SaveImage(object sender)
        {
            SaveFileDialog saveFile = new SaveFileDialog();

            saveFile.DefaultExt = ".png";

            saveFile.Filter = "Pictures (*.jpg;*.gif;*.png)|*.jpg;*.gif;*.png";

            if(saveFile.ShowDialog() == true)
            {
                CreateThumbnail(saveFile.FileName, _image.Clone());
            }
        }

        private bool DoesImageExist(object sender)
        {
            return _image != null;
        }

        private void CreateThumbnail(string filename, BitmapSource image5)
        {
            if (filename != string.Empty)
            {
                using (FileStream stream5 = new FileStream(filename, FileMode.Create))
                {
                    PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                    encoder5.Frames.Add(BitmapFrame.Create(image5));
                    encoder5.Save(stream5);
                }
            }
        }

        private void ComputeImageCenter(BitmapImage image)
        {
            int bytesPerPixel = image.Format.BitsPerPixel / 8;
            int stride = image.PixelWidth * bytesPerPixel;
            int size = image.PixelHeight * stride;
            byte[] pixels = new byte[size];
            image.CopyPixels(pixels, stride, 0);

            byte[,] byteArray = new byte[image.PixelWidth, image.PixelHeight];
            byte[] binaryImage = new byte[image.PixelWidth * image.PixelHeight];
            int average = 0;
            int x, y;
            for (var i = 0; i < pixels.Length; i += bytesPerPixel)
            {
                for (var j = 0; j < bytesPerPixel - 1; j++)
                    average += pixels[i + j];

                x = (i / bytesPerPixel) % image.PixelWidth;
                y = (i / bytesPerPixel) / image.PixelWidth;
                average /= (bytesPerPixel - 1);

                byteArray[x, y] = average > 128 ? (byte)255 : (byte)0;
                binaryImage[i / bytesPerPixel] = average > 128 ? (byte)255 : (byte)0;

                average = 0;
            }

            byte[] newPixels = (byte[])pixels.Clone();
            Coords extDiaAverage = GetExternDiameterAverage(byteArray, image.PixelWidth, image.PixelHeight);
            newPixels[stride * extDiaAverage.y + bytesPerPixel * extDiaAverage.x] = 0;
            newPixels[stride * extDiaAverage.y + bytesPerPixel * extDiaAverage.x + 1] = 0;
            newPixels[stride * extDiaAverage.y + bytesPerPixel * extDiaAverage.x + 2] = 255;
            WriteableBitmap wrBmp = new WriteableBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Bgr32, null);
            wrBmp.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), newPixels, stride, 0);

            Image = wrBmp;
            ImageTemplateWidth = image.PixelWidth;
            ImageTemplateHeight = image.PixelHeight;
            XCenter = extDiaAverage.x.ToString();
            YCenter = extDiaAverage.y.ToString();

            isCenterComputed = true;
        }

        private Coords GetExternDiameterAverage(byte[,] byteArray, int width, int height)
        {
            ulong xTotal = 0, yTotal = 0;
            int pixelCount = 0;

            int xStart = width - 1;
            int xEnd = 0;
            for(int j=0; j<height; j++)
            {
                for(int i=0; i<width; i++)
                {
                    if (byteArray[i, j] == 255)
                    {
                        xStart = i;
                        break;
                    }   
                }

                for(int i=width-1; i>=0; i--)
                {
                    if(byteArray[i,j] == 255)
                    {
                        xEnd = i;
                        break;
                    }
                }

                for(int i=xStart; i<=xEnd; i++)
                {
                    xTotal += (ulong)i;
                    yTotal += (ulong)j;
                    pixelCount++;
                }

                xStart = width - 1;
                xEnd = 0;
            }

            Coords result = new Coords();
            result.x = (int)(xTotal / (ulong)pixelCount);
            result.y = (int)(yTotal / (ulong)pixelCount);
            return result;
        }

        private Coords GetWhiteAverage(byte[,] byteArray, int width, int height)
        {
            ulong xTotal = 0, yTotal = 0;
            int pixelCount = 0;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    if (byteArray[i, j] == 255)
                    {
                        xTotal += (ulong)i;
                        yTotal += (ulong)j;

                        pixelCount++;
                    }
                }
            }

            Coords result = new Coords();
            result.x = (int)(xTotal / (ulong)pixelCount);
            result.y = (int)(yTotal / (ulong)pixelCount);
            return result;
        }

        public event EventHandler Capture;
        public event EventHandler Release;

        public void OnMouseDown(object sender, MouseCaptureArgs e)
        {
        }

        public void OnMouseMove(object sender, MouseCaptureArgs e)
        {
            if (!isCenterComputed)
                return;

            MouseX = (e.X / ImageTemplateWidth * Image.PixelWidth).ToString();
            MouseY = (e.Y / ImageTemplateHeight * Image.PixelHeight).ToString();

            if(isCenterComputed)
            {
                double xDiff = Convert.ToDouble(MouseX) - Convert.ToDouble(XCenter);
                double yDiff = Convert.ToDouble(MouseY) - Convert.ToDouble(YCenter);
                Distance = Math.Round(Math.Sqrt(xDiff * xDiff + yDiff * yDiff), 2);
            }
        }

        public void OnMouseUp(object sender, MouseCaptureArgs e)
        {
        }

        public class Coords
        {
            public int x;
            public int y;
        }
    }
}
