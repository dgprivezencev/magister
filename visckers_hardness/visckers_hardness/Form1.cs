using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;

using System;
using System.Numerics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VectorClass;
using System.Windows.Forms.DataVisualization.Charting;

namespace visckers_hardness
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoCaptureDevices;
        private VideoCaptureDevice videoStream;

        int[,] objectsMap;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OtsuThreshold filter = new OtsuThreshold();
            Grayscale grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap srcBitmap = new Bitmap(sourcePictureBox.Image);
            Bitmap grayImage = grayFilter.Apply(srcBitmap);
            filter.ApplyInPlace(grayImage);
            resultPictureBox.Image = grayImage;
            chart1.Series[0].Points.DataBindY(calculateChart(grayImage));
        }

        public int[] calculateChart(Bitmap image)
        {
            int[] chartMassive = new int[256];
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    chartMassive[((int)(image.GetPixel(x, y).R * 0.33) + (int)(image.GetPixel(x, y).G * 0.33) + (int)(image.GetPixel(x, y).B * 0.33))]++;
                }
            return chartMassive;
        }

        private void medianFilterButton_Click(object sender, EventArgs e)
        {
            Median filter = new Median();
            Bitmap srcBitmap = new Bitmap(sourcePictureBox.Image);
            filter.ApplyInPlace(srcBitmap);
            chart1.Series[0].Points.DataBindY(calculateChart(srcBitmap));
            resultPictureBox.Image = srcBitmap;
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            sourcePictureBox.Image = resultPictureBox.Image;
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                sourcePictureBox.Image = Image.FromFile(dialog.FileName);
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = "png";
            saveDialog.Filter = "Изображение |*.png";
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                sourcePictureBox.Image.Save(saveDialog.FileName);
            }
        }

        private void медианнаяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Median filter = new Median();
            Bitmap srcBitmap = new Bitmap(sourcePictureBox.Image);
            filter.ApplyInPlace(srcBitmap);
            chart1.Series[0].Points.DataBindY(calculateChart(srcBitmap));
            resultPictureBox.Image = srcBitmap;
        }

        private void otsuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OtsuThreshold otsuFilter = new OtsuThreshold(); //Фильтр, котор
            Grayscale grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap srcBitmap = new Bitmap(sourcePictureBox.Image);
            Bitmap grayImage = grayFilter.Apply(srcBitmap);
            int threshold = otsuFilter.CalculateThreshold(grayImage, new Rectangle(0, 0, grayImage.Width, grayImage.Height)) + trackBar1.Value;
            int delta = 10;
            Threshold filter = new Threshold(threshold - delta);
            filter.ApplyInPlace(grayImage);
            resultPictureBox.Image = grayImage;
            chart1.Series[0].Points.DataBindY(calculateChart(grayImage));
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            sourcePictureBox.Image = resultPictureBox.Image;
        }


        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //splitContainer1.SplitterDistance = splitContainer1.Width / 2;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            videoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoCaptureDevice in videoCaptureDevices)
            {
                toolStripComboBox1.Items.Add(videoCaptureDevice.Name);
            }

            toolStripComboBox1.SelectedIndex = 0;
            videoStream = new VideoCaptureDevice();

            label4.Text = "0";
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (videoStream.IsRunning == true)
            {
                videoStream.Stop();
                return;
            }

            videoStream = new VideoCaptureDevice(videoCaptureDevices[toolStripComboBox1.SelectedIndex].MonikerString);
            videoStream.NewFrame += new NewFrameEventHandler(VideoStream_NewFrame);

            videoStream.Start();
        }

        private void VideoStream_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap video = (Bitmap)eventArgs.Frame.Clone();
            sourcePictureBox.Image = video;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (videoStream.IsRunning == true) videoStream.Stop();
        }

        private void поПлощадиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap srcBitmap = new Bitmap(sourcePictureBox.Image);
            objectsMap = new int[srcBitmap.Width, srcBitmap.Height];
            int objectCounter = 1; //Счетчик объектов
            int pointsCount = 0; //Счетчик пикселей, из которых состоит объект
            List<Vector2D_Int> objectPoints = new List<Vector2D_Int>(); //Список для рекурсивного обхода точек по объекту
            Dictionary<int, int> objectsSpaces = new Dictionary<int, int>();
            int max = 0; //Количество точек в самом больщом объекте
            //В цикле проходим по всем пикселям изображения
            for (int x = 0; x < srcBitmap.Width; x++)
            {
                for (int y = 0; y < srcBitmap.Height; y++)
                {
                    //Если встречаем черную точку и данную точку мы еще не посещали,
                    //то начинаем поиск соседних черных точек, чтобы обойти весь объект
                    if ((srcBitmap.GetPixel(x,y).R == 0) && (objectsMap[x,y] == 0))
                    {
                        pointsCount = 1;
                        objectsMap[x, y] = objectCounter; //Заполняем массив, в котором указаны какие точки относятся к какому объекту
                        objectPoints.Add(new Vector2D_Int(x, y)); //Добавляем текущую точку в список для проверки соседних точек
                        while(objectPoints.Count > 0) //Если в списке точек на проверку имеются записи (при первом входе там будет минимум одна запись с текущей точкой
                        {
                            Vector2D_Int currentPoint = objectPoints[0];
                            //Проходим по 8 соседним точкам
                            for (int dy = -1; dy < 2; dy++)
                            {
                                for (int dx = -1; dx < 2; dx++)
                                {
                                    //Проверка на выход текущей точки за пределы изображения
                                    int currentPosX = Math.Min(Math.Max(currentPoint.X + dx, 0), srcBitmap.Width - 1);
                                    int currentPosY = Math.Min(Math.Max(currentPoint.Y + dy, 0), srcBitmap.Height - 1);

                                    byte tempR = srcBitmap.GetPixel(currentPosX, currentPosY).R;
                                    
                                    //Если проверяемая точка черная и мы её не посещали
                                    if ((tempR == 0) && (objectsMap[currentPosX, currentPosY] == 0) && ((dx != 0) || (dy != 0)))
                                    {
                                        //То в массиве объектов указываем отношение данной точки к объекту
                                        objectsMap[currentPosX, currentPosY] = objectCounter;
                                        //Добавляем точку в список для последующей проверки
                                        objectPoints.Add(new Vector2D_Int(currentPosX, currentPosY));
                                        //Увеличиваем счетчик точек в объкте
                                        pointsCount++;
                                    }
                                }
                            }
                            //После проверки текущей точки удаляем её из списка, чтобы на первое место встала следующая точка
                            objectPoints.RemoveAt(0);
                        }
                        //Сохраняем идентификатор и размер объекта
                        objectsSpaces.Add(objectCounter, pointsCount);
                        //Инкрементируем счетик объектов
                        objectCounter++;
                        max = Math.Max(max, pointsCount);
                        pointsCount = 0;
                    }
                }
            }
            pointsCount = 0;
            
            int[] chartSQ = new int[max / 10 + 1]; //Массив для составления гистограммы с распределением по размерам
            
            List<int> listBigObjectsId = new List<int>(); //Список идентификаторов объектов, которые удовлетворяют условию порога
            //Составление списка объектов
            for (int i = 1; i < objectCounter; i++)
            {
                chartSQ[objectsSpaces[i] / 10]++;
            }

            int threshold = Convert.ToInt32((max / 2) / Math.Sqrt(3)); //Порог для отсеивания маленьких объектов
            for (int i = 1; i < objectCounter; i++)
            {
                if (objectsSpaces[i] >= threshold)
                {
                    listBigObjectsId.Add(i);
                }
            }
            Bitmap resultImage = new Bitmap(srcBitmap.Width, srcBitmap.Height);

            //Составление изображение на основе списка тех объектов, которые прошли порог
            for (int x = 0; x < srcBitmap.Width; x++)
            {
                for (int y = 0; y < srcBitmap.Height; y++)
                {
                    if (listBigObjectsId.Contains(objectsMap[x, y]))
                    {
                        resultImage.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        objectsMap[x, y] = 0;
                        resultImage.SetPixel(x, y, Color.White);
                    }
                }
            }
            //Формирование гистограммы соотношения количества объектов к их площадям
            int PointCount = 1;
            chart1.Series[0].Points.Clear();
            for (int i = 0; i < chartSQ.Length; i++)
            {
                if (chartSQ[i] > 0)
                {
                    DataPoint data = new DataPoint(PointCount++, chartSQ[i]);
                    data.Label = (i * 10).ToString();
                    chart1.Series[0].Points.Add(data);
                }
            }
            resultPictureBox.Image = resultImage;            
        }

        private void splitContainer1_Panel1_Scroll(object sender, ScrollEventArgs e)
        {
            splitContainer1.Panel2.VerticalScroll.Value = splitContainer1.Panel1.VerticalScroll.Value;
            splitContainer1.Panel2.HorizontalScroll.Value = splitContainer1.Panel1.HorizontalScroll.Value;
        }

        private void splitContainer1_Panel2_Scroll(object sender, ScrollEventArgs e)
        {
            splitContainer1.Panel1.VerticalScroll.Value = splitContainer1.Panel2.VerticalScroll.Value;
            splitContainer1.Panel1.HorizontalScroll.Value = splitContainer1.Panel2.HorizontalScroll.Value;
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label4.Text = trackBar1.Value.ToString();
        }

        private void sourcePictureBox_Click(object sender, EventArgs e)
        {

        }

        private void поФигуреToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap srcBitmap = new Bitmap(sourcePictureBox.Image);
            objectsMap = new int[srcBitmap.Width, srcBitmap.Height];
            int objectCounter = 1; //Счетчик объектов
            int pointsCount = 0; //Счетчик пикселей, из которых состоит объект
            List<Vector2D_Int> objectPoints = new List<Vector2D_Int>(); //Список для рекурсивного обхода точек по объекту
            Dictionary<int, int> objectsSpaces = new Dictionary<int, int>();
            int max = 0; //Количество точек в самом больщом объекте
            //В цикле проходим по всем пикселям изображения
            for (int x = 0; x < srcBitmap.Width; x++)
            {
                for (int y = 0; y < srcBitmap.Height; y++)
                {
                    //Если встречаем черную точку и данную точку мы еще не посещали, 
                    //то начинаем поиск соседних черных точек, чтобы обойти весь объект
                    if ((srcBitmap.GetPixel(x, y).R == 0) && (objectsMap[x, y] == 0))
                    {
                        pointsCount = 1;
                        objectsMap[x, y] = objectCounter; //Заполняем массив, в котором указаны какие точки относятся к какому объекту
                        objectPoints.Add(new Vector2D_Int(x, y)); //Добавляем текущую точку в список для проверки соседних точек
                        while (objectPoints.Count > 0) //Если в списке точек на проверку имеются записи (при первом входе там будет минимум одна запись с текущей точкой
                        {
                            Vector2D_Int currentPoint = objectPoints[0];
                            //Проходим по 8 соседним точкам
                            for (int dy = -1; dy < 2; dy++)
                            {
                                for (int dx = -1; dx < 2; dx++)
                                {
                                    //Проверка на выход текущей точки за пределы изображения
                                    int currentPosX = Math.Min(Math.Max(currentPoint.X + dx, 0), srcBitmap.Width - 1);
                                    int currentPosY = Math.Min(Math.Max(currentPoint.Y + dy, 0), srcBitmap.Height - 1);

                                    byte tempR = srcBitmap.GetPixel(currentPosX, currentPosY).R;

                                    //Если проверяемая точка черная и мы её не посещали
                                    if ((tempR == 0) && (objectsMap[currentPosX, currentPosY] == 0) && ((dx != 0) || (dy != 0)))
                                    {
                                        //То в массиве объектов указываем отношение данной точки к объекту
                                        objectsMap[currentPosX, currentPosY] = objectCounter;
                                        //Добавляем точку в список для последующей проверки
                                        objectPoints.Add(new Vector2D_Int(currentPosX, currentPosY));
                                        //Увеличиваем счетчик точек в объкте
                                        pointsCount++;
                                    }
                                }
                            }
                            //После проверки текущей точки удаляем её из списка, чтобы на первое место встала следующая точка
                            objectPoints.RemoveAt(0);
                        }
                        //Сохраняем идентификатор и размер объекта
                        objectsSpaces.Add(objectCounter, pointsCount);
                        //Инкрементируем счетик объектов
                        objectCounter++;
                        max = Math.Max(max, pointsCount);
                        pointsCount = 0;
                    }
                }
            }
            pointsCount = 0;
            List<int> listObjectsId = new List<int>(); //Список идентификаторов объектов, которые удовлетворяют условию порога

            boundingBox[] boundingBoxesObject = new boundingBox[objectsSpaces.Count];

            for (int i = 0; i < boundingBoxesObject.Length; i++)
            {
                boundingBoxesObject[i] = new boundingBox(srcBitmap.Width, srcBitmap.Height);
            }

            for (int x = 0; x < srcBitmap.Width; x++)
            {
                for (int y = 0; y < srcBitmap.Height; y++)
                {
                    if (objectsMap[x,y] != 0)
                    {
                        int idObject = objectsMap[x, y] - 1;
                        boundingBoxesObject[idObject].maxX = Math.Max(x, boundingBoxesObject[idObject].maxX);
                        boundingBoxesObject[idObject].minX = Math.Min(x, boundingBoxesObject[idObject].minX);
                        boundingBoxesObject[idObject].maxY = Math.Max(y, boundingBoxesObject[idObject].maxY);
                        boundingBoxesObject[idObject].minY = Math.Min(y, boundingBoxesObject[idObject].minY);
                    }
                }
            }

            for (int i = 0; i < boundingBoxesObject.Length; i++)
            {
                int square = (boundingBoxesObject[i].maxX - boundingBoxesObject[i].minX) * (boundingBoxesObject[i].maxY - boundingBoxesObject[i].minY);
                double squareTreshold = Convert.ToDouble(objectsSpaces[i+1]) / Convert.ToDouble(square);
                if (squareTreshold > 0.45)
                {
                    listObjectsId.Add(i + 1);
                }
            }
            Bitmap resultImage = new Bitmap(srcBitmap.Width, srcBitmap.Height);

            //Составление изображение на основе списка тех объектов, которые прошли порог
            for (int x = 0; x < srcBitmap.Width; x++)
            {
                for (int y = 0; y < srcBitmap.Height; y++)
                {
                    if (listObjectsId.Contains(objectsMap[x, y]))
                    {
                        resultImage.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        objectsMap[x, y] = 0;
                        resultImage.SetPixel(x, y, Color.White);
                    }
                }
            }
            resultPictureBox.Image = resultImage;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения (*.BMP;*.JPG)|*.BMP;*.JPG";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var result = processImage(dialog.FileName, trackBar1.Value);
                resultPictureBox.Image = result[1];
                sourcePictureBox.Image = result[0];
            }
            
            
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2.VerticalScroll.Value = 0;
            splitContainer1.Panel2.HorizontalScroll.Value = 0;
            splitContainer1.Panel1.VerticalScroll.Value = 0;
            splitContainer1.Panel1.HorizontalScroll.Value = 0;
            if (sourcePictureBox.SizeMode == PictureBoxSizeMode.AutoSize)
            {
                sourcePictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                resultPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            else
            {
                sourcePictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
                resultPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            }
        }

        public Bitmap[] processImage(string filePath, int thresholdValueFromTrackBar)
        {
            OtsuThreshold otsuFilter = new OtsuThreshold(); //Фильтр, котор
            Median medianFilter = new Median();
            Grayscale grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap srcBitmap = new Bitmap(filePath);
            Bitmap resultImage = grayFilter.Apply(srcBitmap);
            int threshold = otsuFilter.CalculateThreshold(resultImage, new Rectangle(0, 0, resultImage.Width, resultImage.Height)) + thresholdValueFromTrackBar;
            int delta = 10;
            Threshold filter = new Threshold(threshold - delta);
            filter.ApplyInPlace(resultImage);
            medianFilter.ApplyInPlace(resultImage);
            objectsMap = new int[resultImage.Width, resultImage.Height];
            int objectCounter = 1; //Счетчик объектов
            int pointsCount = 0; //Счетчик пикселей, из которых состоит объект
            List<Vector2D_Int> objectPoints = new List<Vector2D_Int>(); //Список для рекурсивного обхода точек по объекту
            Dictionary<int, int> objectsSpaces = new Dictionary<int, int>();
            int max = 0; //Количество точек в самом больщом объекте
            //В цикле проходим по всем пикселям изображения
            for (int x = 0; x < resultImage.Width; x++)
            {
                for (int y = 0; y < resultImage.Height; y++)
                {
                    //Если встречаем черную точку и данную точку мы еще не посещали, 
                    //то начинаем поиск соседних черных точек, чтобы обойти весь объект
                    if ((resultImage.GetPixel(x, y).R == 0) && (objectsMap[x, y] == 0))
                    {
                        pointsCount = 1;
                        objectsMap[x, y] = objectCounter; //Заполняем массив, в котором указаны какие точки относятся к какому объекту
                        objectPoints.Add(new Vector2D_Int(x, y)); //Добавляем текущую точку в список для проверки соседних точек
                        while (objectPoints.Count > 0) //Если в списке точек на проверку имеются записи (при первом входе там будет минимум одна запись с текущей точкой
                        {
                            Vector2D_Int currentPoint = objectPoints[0];
                            //Проходим по 8 соседним точкам
                            for (int dy = -1; dy < 2; dy++)
                            {
                                for (int dx = -1; dx < 2; dx++)
                                {
                                    //Проверка на выход текущей точки за пределы изображения
                                    int currentPosX = Math.Min(Math.Max(currentPoint.X + dx, 0), resultImage.Width - 1);
                                    int currentPosY = Math.Min(Math.Max(currentPoint.Y + dy, 0), resultImage.Height - 1);

                                    byte tempR = resultImage.GetPixel(currentPosX, currentPosY).R;

                                    //Если проверяемая точка черная и мы её не посещали
                                    if ((tempR == 0) && (objectsMap[currentPosX, currentPosY] == 0) && ((dx != 0) || (dy != 0)))
                                    {
                                        //То в массиве объектов указываем отношение данной точки к объекту
                                        objectsMap[currentPosX, currentPosY] = objectCounter;
                                        //Добавляем точку в список для последующей проверки
                                        objectPoints.Add(new Vector2D_Int(currentPosX, currentPosY));
                                        //Увеличиваем счетчик точек в объкте
                                        pointsCount++;
                                    }
                                }
                            }
                            //После проверки текущей точки удаляем её из списка, чтобы на первое место встала следующая точка
                            objectPoints.RemoveAt(0);
                        }
                        //Сохраняем идентификатор и размер объекта
                        objectsSpaces.Add(objectCounter, pointsCount);
                        //Инкрементируем счетик объектов
                        objectCounter++;
                        max = Math.Max(max, pointsCount);
                        pointsCount = 0;
                    }
                }
            }
            pointsCount = 0;
            List<int> listObjectsId = new List<int>(); //Список идентификаторов объектов, которые удовлетворяют условию порога

            boundingBox[] boundingBoxesObject = new boundingBox[objectsSpaces.Count];

            for (int i = 0; i < boundingBoxesObject.Length; i++)
            {
                boundingBoxesObject[i] = new boundingBox(resultImage.Width, resultImage.Height);
            }

            for (int x = 0; x < resultImage.Width; x++)
            {
                for (int y = 0; y < resultImage.Height; y++)
                {
                    if (objectsMap[x, y] != 0)
                    {
                        int idObject = objectsMap[x, y] - 1;
                        boundingBoxesObject[idObject].maxX = Math.Max(x, boundingBoxesObject[idObject].maxX);
                        boundingBoxesObject[idObject].minX = Math.Min(x, boundingBoxesObject[idObject].minX);
                        boundingBoxesObject[idObject].maxY = Math.Max(y, boundingBoxesObject[idObject].maxY);
                        boundingBoxesObject[idObject].minY = Math.Min(y, boundingBoxesObject[idObject].minY);
                    }
                }
            }
            max = 0;

            for (int i = 0; i < boundingBoxesObject.Length; i++)
            {
                int square = (boundingBoxesObject[i].maxX - boundingBoxesObject[i].minX) * (boundingBoxesObject[i].maxY - boundingBoxesObject[i].minY);
                double squareTreshold = Convert.ToDouble(objectsSpaces[i + 1]) / Convert.ToDouble(square);
                if (squareTreshold > 0.45)
                {
                    max = Math.Max(max, objectsSpaces[i + 1]);
                    listObjectsId.Add(i + 1);
                }
            }

            int sizeThreshold = Convert.ToInt32((max / 2) / Math.Sqrt(3)); //Порог для отсеивания маленьких объектов
            List<int> resultListObjectsId = new List<int>();
            foreach (int i in listObjectsId)
            {
                if (objectsSpaces[i] > sizeThreshold)
                {
                    resultListObjectsId.Add(i);
                }
            }

            Bitmap result = new Bitmap(resultImage.Width, resultImage.Height);

            //Составление изображение на основе списка тех объектов, которые прошли порог
            for (int x = 0; x < resultImage.Width; x++)
            {
                for (int y = 0; y < resultImage.Height; y++)
                {
                    if (resultListObjectsId.Contains(objectsMap[x, y]))
                    {
                        result.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        objectsMap[x, y] = 0;
                        result.SetPixel(x, y, Color.White);
                    }
                }
            }


            boundingBox targetBox = boundingBoxesObject[resultListObjectsId[0] - 1];

            int squareObject = (targetBox.maxX - targetBox.minX) * (targetBox.maxY - targetBox.minY);
            int perimetrObject = ((targetBox.maxX - targetBox.minX) + (targetBox.maxY - targetBox.minY)) * 2;
            int diagonalObject = Convert.ToInt32(Math.Pow(Math.Pow(Convert.ToDouble(targetBox.maxX - targetBox.minX), 2) + Math.Pow(Convert.ToDouble(targetBox.maxY - targetBox.minY), 2), 0.5));
            string messageText = "Площадь объекта: " + squareObject + "\n" +
                "Периметр объекта: " + perimetrObject + "\n" + "Диагональ объекта: " + diagonalObject;
            //MessageBox.Show(messageText, "Данные об объекте", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Graphics gResult = Graphics.FromImage(result);
            Graphics gSource = Graphics.FromImage(srcBitmap);
            Pen pen = new Pen(Color.DarkBlue, 3);
            gResult.DrawRectangle(pen, targetBox.getRectangle());

            string diagonalInfo = "d1 = " + diagonalObject.ToString() + "pxl; d2 = " + diagonalObject.ToString() + "pxl";

            Font drawFont = new Font("Arial", 16);
            SolidBrush drawBrush = new SolidBrush(Color.DarkBlue);
            gResult.DrawString(diagonalInfo, drawFont, drawBrush, targetBox.minX, targetBox.minY - 25);
            gSource.DrawString(diagonalInfo, drawFont, drawBrush, targetBox.minX, targetBox.minY - 25);
            gSource.DrawRectangle(pen, targetBox.getRectangle());
            gResult.Save();
            gSource.Save();

            Bitmap[] resultedImages = new Bitmap[2];

            resultedImages[1] = result;
            resultedImages[0] = srcBitmap;
            return resultedImages;
        }

        private void открытьНесколькоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Изображения (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in dialog.FileNames)
                {
                    string temp = file.Substring(0, file.Length - 4) + "_before" + file.Substring(file.Length - 4, 4);
                    var result = processImage(file, trackBar1.Value);
                    Image resultBefore = result[0];
                    resultBefore.Save(file.Substring(0, file.Length - 4) + "_before" + file.Substring(file.Length - 4, 4));
                    Image resultAfter = result[1];
                    resultAfter.Save(file.Substring(0, file.Length - 4) + "_after" + file.Substring(file.Length - 4, 4));
                }
                MessageBox.Show("Харооош", "ЕЕЕЕЕЕЕЕЕеее", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
         
        }

        private void Capture_ImageGrabbed(object sender, EventArgs e)
        {
           
        }
    }
}
