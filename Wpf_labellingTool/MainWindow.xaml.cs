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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Wpf_labellingTool
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        Mat mat_show;//= new Mat(480, 640, DepthType.Cv8U, 3);

        VideoCapture webCam;
        Mat mat_cam = new Mat(480, 640, DepthType.Cv8U, 3);
        bool liveLoop = false;

        Mat mat_LoadImage;//= new Mat(480, 640, DepthType.Cv8U, 3);
        Mat mat_guideLine;// = new Mat(480, 640, DepthType.Cv8U, 3);
        Mat mat_boundBox;//= new Mat(480, 640, DepthType.Cv8U, 3);
        Mat mat_boundBox_tmp;// = new Mat(480, 640, DepthType.Cv8U, 3);
        System.Drawing.Size loadImageSize = new System.Drawing.Size();

        static ObservableCollection<ListViewData> ListViewDataCollection = new ObservableCollection<ListViewData>();
        public MainWindow()
        {
            InitializeComponent();
            lv_tranningList.ItemsSource = ListViewDataCollection;
            //DispatcherTimer _timer = new DispatcherTimer();
            //_timer.Interval = TimeSpan.FromMilliseconds(50);
            //_timer.Tick += timeCycle;
            //_timer.Start();
        }
        #region unUse
        enum ShowMode
        {
            none = 0,
            camera = 1,
            video = 2,
            file = 3
        }
        ShowMode Mode = ShowMode.none;
        public void timeCycle(object sender, EventArgs e)
        {
            if (Mode == ShowMode.none)
            {

            }
            else if (Mode == ShowMode.camera)
            {

            }
            else if (Mode == ShowMode.file)
            {
                mat_LoadImage.CopyTo(mat_show);
                mat_show.AddLayer(mat_guideLine);
                img_main.Source = BitmapSourceConvert.MatToBitmap(mat_show);
                Console.WriteLine("tick");
            }
        }
        #endregion unUse
        private void ImageUpdate(bool alreadyHandle)
        {
            if (alreadyHandle == false)
            {
                mat_LoadImage.CopyTo(mat_show);
                mat_show.AddLayer(mat_boundBox);
                mat_show.AddLayer(mat_boundBox_tmp);
                mat_show.AddLayer(mat_guideLine);
                img_main.Source = BitmapSourceConvert.MatToBitmap(mat_show);
            }

        }

        //take photo
        private void Btn_openCamera_Click(object sender, RoutedEventArgs e)
        {
            webCam = new VideoCapture(0);
            liveLoop = true;

            Task.Run(() =>
            {
                Mode = ShowMode.camera;
                while (liveLoop)
                {
                    mat_cam = webCam.QueryFrame();

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        img_main.Source = BitmapSourceConvert.MatToBitmap(mat_cam);
                    }));
                }
            });
        }

        private void Btn_saveImage_Click(object sender, RoutedEventArgs e)
        {
            //File.Exists(curFile)
            string folder = tb_saveImage_folder.Text;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            CvInvoke.Imwrite($"{folder}//{tb_saveImage_pretext.Text}{tb_saveImage_count.Text}.png", mat_show);
            tb_saveImage_count.Text = (int.Parse(tb_saveImage_count.Text) + 1).ToString();
        }
        private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        string OpenFolder = "";
        FileInfo[] OpenFiles;
        int OpenFilesIndex = 0;
        List<LabellingInfo> LabelList = new List<LabellingInfo>();

        //---Load---//
        private void Btn_loadFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.ShowDialog();

            OpenFolder = path.SelectedPath;
            LoadFolder();
        }
        private void Btn_loadFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Count() > 1)
                {
                    MessageBox.Show("one file only");
                    return;
                }

                OpenFolder = files[0];
                LoadFolder();
            }
        }
         void LoadFolder()
        {
            if (OpenFolder == "")
            {
                Mode = ShowMode.none;
                return;
            }
            Mode = ShowMode.file;
            lb_selectLoadFolder.Content = $"Load Path : " + OpenFolder;

            DirectoryInfo OpenDirectory = new DirectoryInfo(OpenFolder);//Assuming Test is your Folder
            OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
            if (OpenFiles.Count() == 0)
            {
                MessageBox.Show("沒有png");
                lb_loadImage.Content = "Load Image : N/A";
                return;
            }
            liveLoop = false;

            //zero something
            OpenFilesIndex = 0;

            Btn_reLoadImage_Click(null,null);
        }

        //---next image---//
        private void Btn_nextImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFilesIndex++;
            if (OpenFiles.Count() == OpenFilesIndex)
            {
                MessageBox.Show("Last image");
                OpenFilesIndex--;
                return;
            }
            Btn_reLoadImage_Click(sender, e);
        }
        private void Btn_previousImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFilesIndex--;
            if (OpenFilesIndex < 0)
            {
                MessageBox.Show("First image");
                OpenFilesIndex++;
                return;
            }
            Btn_reLoadImage_Click(sender, e);
        }
        private void Btn_reLoadImage_Click(object sender, RoutedEventArgs e)
        {
            boundtype = BoundBox.none;
            LabelList = new List<LabellingInfo>();
            lb_loadImage.Content = $"Load Image({OpenFilesIndex + 1}/{OpenFiles.Count()}) : " + OpenFiles[OpenFilesIndex].Name;
            mat_LoadImage = CvInvoke.Imread(OpenFiles[OpenFilesIndex].FullName);

            loadImageSize = mat_LoadImage.Size;
            mat_show = new Mat(loadImageSize, DepthType.Cv8U, 3);
            mat_guideLine = new Mat(loadImageSize, DepthType.Cv8U, 3);
            mat_boundBox_tmp = new Mat(loadImageSize, DepthType.Cv8U, 3);
            mat_boundBox = new Mat(loadImageSize, DepthType.Cv8U, 3);
            if (cb_loadOldLabel.IsChecked == true)//如果要讀已經lebel過的檔案
            {
                string fileName = OpenFiles[OpenFilesIndex].FullName.Replace(".png", ".txt");
                if (System.IO.File.Exists(fileName) == true)
                {
                    string[] allString = System.IO.File.ReadAllLines(fileName);
                    for (int line = 0; line < allString.Count(); line++)
                    {
                        int index = allString[line].Split(' ')[0].ToInt();
                        double x_center = allString[line].Split(' ')[1].ToDouble();
                        double y_center = allString[line].Split(' ')[2].ToDouble();
                        double width = allString[line].Split(' ')[3].ToDouble();
                        double height = allString[line].Split(' ')[4].ToDouble();

                        Rectangle rect = new Rectangle();
                        rect.Width = (int)(width * loadImageSize.Width);
                        rect.Height = (int)(height * loadImageSize.Height);
                        rect.X = (int)((x_center * loadImageSize.Width) - (rect.Width / 2));
                        rect.Y = (int)((y_center * loadImageSize.Height) - (rect.Height / 2));
                        LabelList.Add(new LabellingInfo(index, rect));
                    }
                    DrawLabellingBox();
                }
            }
            ImageUpdate(threadLoop);
        }
        private void Btn_saveLabelling_Click(object sender, RoutedEventArgs e)
        {
            if (boundtype == BoundBox.done || boundtype == BoundBox.none)
            {
                string name = OpenFiles[OpenFilesIndex].Name.Substring(0, OpenFiles[OpenFilesIndex].Name.IndexOf("."));
                StreamWriter txt = new StreamWriter($"{OpenFolder}//{name}.txt", false);
                foreach (LabellingInfo info in LabelList)
                {
                    if (info.Index >= 0)
                    {
                        //<x_center> <y_center> <width> <height> - float values relative to width and height of image, it can be equal from (0.0 to 1.0]
                        double x_center = (info.boundingbox.X + info.boundingbox.Width / 2.0) / (double)loadImageSize.Width;
                        if (x_center > 1) x_center = 1; if (x_center < 0) x_center = 0.0001;//防止超過邊線(0,1]
                        double y_center = (info.boundingbox.Y + info.boundingbox.Height / 2.0) / (double)loadImageSize.Height;
                        if (y_center > 1) y_center = 1; if (y_center < 0) y_center = 0.0001;//防止超過邊線(0,1]
                        double width =Math.Abs( ((double)info.boundingbox.Width / (double)loadImageSize.Width));//正負不影響center值(但規定要正值)
                        double height = Math.Abs(((double)info.boundingbox.Height / (double)loadImageSize.Height));
                        string str = $"{info.Index.ToString()} {x_center.ToString("0.0000")} {y_center.ToString("0.0000")} {width.ToString("0.0000")} {height.ToString("0.0000")}";

                        txt.WriteLine(str);
                    }
                    else//代表沒有定義index過
                    {
                        //有問題!?
                    }

                }
                txt.Flush();
                txt.Close();
                CvInvoke.Rectangle(mat_boundBox, new Rectangle(0, 0, loadImageSize.Width, loadImageSize.Height), new MCvScalar(100, 200, 110), 10);
                ImageUpdate(threadLoop);
            }
            else
            {
                MessageBox.Show("Please finish labeling");
            }

        }
        private void Btn_clearLabel_Click(object sender, RoutedEventArgs e)
        {
            LabelList = new List<LabellingInfo>();
            DrawLabellingBox();
            ImageUpdate(threadLoop);
        }
        public void DrawLabellingBox()
        {
            mat_boundBox = new Mat(loadImageSize, DepthType.Cv8U, 3);
            mat_boundBox_tmp = new Mat(loadImageSize, DepthType.Cv8U, 3);
            setToZero(ref mat_boundBox);
            foreach (LabellingInfo info in LabelList)
            {
                if (info.Index >= 0)
                {
                    CvInvoke.Rectangle(mat_boundBox, info.boundingbox, new MCvScalar(100, 200, 110), 2);
                    CvInvoke.PutText(mat_boundBox, $"[{info.Index.ToString()}]", info.boundingbox.Location, FontFace.HersheySimplex, 0.5, new MCvScalar(100, 200, 110), 1);
                }
                else//代表沒有定義index過
                {
                    CvInvoke.Rectangle(mat_boundBox_tmp, info.boundingbox, new MCvScalar(100, 150, 255), 2);
                }
            }
        }
        public void CleanLabel()
        {
            if (LabelList.Count == 0)
                return;
            mat_boundBox_tmp = new Mat(loadImageSize, DepthType.Cv8U, 3);
            if (LabelList.Last().Index < 0)//代表沒定義過，還在set 階段，就去除
            {
                boundtype = BoundBox.none;
                LabelList.Remove(LabelList.Last());
            }

        }

        #region //---key---\\
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
        }
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            this.KeyDown -= new KeyEventHandler(OnButtonKeyDown);
        }
        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {

            if (Mode == ShowMode.file)
            {
                if (boundtype == BoundBox.set)
                {
                    string str = e.Key.ToString();
                    if (str.IndexOf("NumPad") >= 0)
                    {
                        LabelList.Last().Index = int.Parse(str.Substring(6));
                        DrawLabellingBox();

                        boundtype = BoundBox.done;
                        ImageUpdate(threadLoop);
                    }
                    else
                    {
                        if (e.Key == Key.R)
                        {
                            CleanLabel();
                            DrawLabellingBox();
                            ImageUpdate(threadLoop);
                        }
                        else
                            MessageBox.Show("Please enter the label or cancle");
                    }

                }
                else if (boundtype == BoundBox.done || boundtype == BoundBox.none)//hot key
                {
                    if (e.Key == Key.S)
                        Btn_saveLabelling_Click(null, null);
                    else if (e.Key == Key.D)
                        Btn_nextImage_Click(null, null);
                    else if (e.Key == Key.E)
                        Btn_previousImage_Click(null, null);
                    else if (e.Key == Key.R)
                        Btn_reLoadImage_Click(null, null);
                    else if (e.Key == Key.C)
                        Btn_clearLabel_Click(null, null);
                }
            }
        }
        #endregion \\---key---//

        #region //---image mouse---\\
        enum BoundBox
        {
            none = 0,
            mouseDown = 1,
            mouseDrag = 2,//100, 200, 255
            set = 3, //100, 150, 255
            done = 4//100, 200, 110
        }
        Point mousePoint = new Point();
        BoundBox boundtype = BoundBox.none;
        Point mousePress = new Point();
        Point mouseRelease = new Point();
        private void Img_main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mousePoint = new Point((int)e.GetPosition((IInputElement)sender).X, (int)e.GetPosition((IInputElement)sender).Y);
            mousePress = mousePoint;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Mode == ShowMode.file)//避免在攝影機模式下使用labeling
                {
                    if (boundtype == BoundBox.set)//如果已經是set狀態(上一個沒有label完成)，就要重新刪掉上個未完成的框
                    {
                        LabelList.Remove(LabelList.Last());
                        DrawLabellingBox();
                    }
                    boundtype = BoundBox.mouseDown;
                    ImageUpdate(threadLoop);
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                CleanLabel();
                DrawLabellingBox();
                ImageUpdate(threadLoop);
            }

        }
        private void Img_main_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mousePoint = new Point((int)e.GetPosition((IInputElement)sender).X, (int)e.GetPosition((IInputElement)sender).Y);
            mouseRelease = mousePoint;
            if (Mode == ShowMode.file)
            {
                boundtype = BoundBox.set;
                Rectangle rect = new Rectangle(mousePress.X, mousePress.Y, mouseRelease.X - mousePress.X, mouseRelease.Y - mousePress.Y);
                LabelList.Add(new LabellingInfo(-1, rect));

                if (cb_lockIndex.IsChecked == true)//如果有lock index
                {
                    LabelList.Last().Index = int.Parse(tb_lockIndex.Text);
                    boundtype = BoundBox.done;
                }

                //draw boundBox
                DrawLabellingBox();
                ImageUpdate(threadLoop);
            }
        }

        private void Img_main_MouseMove(object sender, MouseEventArgs e)
        {
            mousePoint = new Point((int)e.GetPosition((IInputElement)sender).X, (int)e.GetPosition((IInputElement)sender).Y);

            if (Mode == ShowMode.file)
            {
                mat_guideLine = new Mat(loadImageSize, DepthType.Cv8U, 3);
                CvInvoke.Line(mat_guideLine, new Point(0, mousePoint.Y), new Point(loadImageSize.Width, mousePoint.Y), new MCvScalar(50, 50, 230));
                CvInvoke.Line(mat_guideLine, new Point(mousePoint.X, 0), new Point(mousePoint.X, loadImageSize.Height), new MCvScalar(50, 50, 230));

                if (boundtype == BoundBox.mouseDown)
                {
                    mat_boundBox_tmp = new Mat(loadImageSize, DepthType.Cv8U, 3);
                    Rectangle rect = new Rectangle(mousePress.X, mousePress.Y, mousePoint.X - mousePress.X, mousePoint.Y - mousePress.Y);
                    CvInvoke.Rectangle(mat_boundBox_tmp, rect, new MCvScalar(100, 200, 255), 2);
                }
            }
            ImageUpdate(threadLoop);
        }

        #endregion \\---image mouse---//

        public static void setToZero(ref Mat mat)
        {
            byte[] value = new byte[mat.Rows * mat.Cols * mat.ElementSize];
            Marshal.Copy(value, 0, mat.DataPointer, mat.Rows * mat.Cols * mat.ElementSize);
        }

        bool threadLoop = false;
        private void Cb_threadShow_Click(object sender, RoutedEventArgs e)
        {
            if (cb_threadShow.IsChecked == true)
            {
                if (mat_LoadImage == null)
                {
                    cb_threadShow.IsChecked = false;
                    MessageBox.Show("Load img == null");
                    return;
                }

                threadLoop = true;
                Task.Run(() =>
                {
                    while (threadLoop == true)
                    {
                        Dispatcher.Invoke((Action)(() => { ImageUpdate(false); }));

                        Thread.Sleep(10);
                    }
                });
            }
            else
            {
                threadLoop = false;
            }

        }


        private void Btn_openFolder_train_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@".\");
        }

        #region //trainning file\\
        private void Btn_addFloder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.ShowDialog();
            string SelectFolder = path.SelectedPath;
            if (SelectFolder == "")
                return;

            DirectoryInfo OpenDirectory = new DirectoryInfo(SelectFolder);
            OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
            if (OpenFiles.Count() == 0)
            {
                MessageBox.Show("沒有png");
                return;
            }
            string addStr = SelectFolder.Substring(SelectFolder.LastIndexOf("\\") + 1);
            addStr += "/";
            ListViewDataCollection.Add(new ListViewData(addStr, SelectFolder, OpenFiles.Count()));
        }

        private void Lv_tranningList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string folderPath in files)
                {
                    DirectoryInfo OpenDirectory = new DirectoryInfo(folderPath);
                    try
                    {
                        OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
                    }
                    catch
                    {
                        MessageBox.Show("Drop in root Folder! NOT files!");
                        return;
                    }

                    string addStr = folderPath.Substring(folderPath.LastIndexOf("\\") + 1);
                    addStr += "/";
                    ListViewDataCollection.Add(new ListViewData(addStr, folderPath, OpenFiles.Count()));
                }
            }
        }

        private void Btn_creatTrainFile_Click(object sender, RoutedEventArgs e)
        {
            //TODO:應該要確認圖片有沒有配合txt 沒有的話就不要加入
            StreamWriter sw = new StreamWriter("All.txt", false);

            //each row of listView
            foreach (ListViewData lv in ListViewDataCollection)
            {
                DirectoryInfo OpenDirectory = new DirectoryInfo(lv.realPath);
                OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
                if (tb_preText.Text.LastIndexOf("/") != tb_preText.Text.Count() - 1)
                    tb_preText.Text += "/";
                //each row of png in folder
                foreach (FileInfo fi in OpenFiles)
                {
                    if (File.Exists(fi.FullName.Replace(".png", ".txt")) == true)
                        sw.WriteLine($"{tb_preText.Text}{lv.Folder}{fi.Name}");
                }
            }
            sw.Flush();
            sw.Close();
        }
        #endregion \\trainning file//

        #region //testing file\\
        private void Slider_trainninPercent_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine(Slider_trainninPercent.Value.ToString());
        }

        private void Slider_trainninPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Console.WriteLine(Slider_trainninPercent.Value.ToString());
            int TrainPer = (int)((100 - Slider_trainninPercent.Value));

            if (tb_trainninPercent != null)
                tb_trainninPercent.Text = $"{TrainPer.ToString()} / {(100 - TrainPer).ToString()}";
        }

        private void Tb_trainFilePath_PreviewDragOver(object sender, DragEventArgs e)
        {//textbox Drag會被搶走，所以要拿回來
            e.Handled = true;
        }
        private void Tb_trainFilePath_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Count() > 1)
                {
                    MessageBox.Show("one file only");
                    return;
                }
                tb_trainFilePath.Text = files[0];
            }
        }

        private void Btn_creatTest_Click(object sender, RoutedEventArgs e)
        {
            string[] allString = System.IO.File.ReadAllLines(tb_trainFilePath.Text);
            double testPer = Slider_trainninPercent.Value;
            int gap = (int)(100.0 / testPer);//每隔多少取一個

            StreamWriter sw_train = new StreamWriter("train.txt", false);
            StreamWriter sw_test = new StreamWriter("test.txt", false);

            int count = 1;
            for (int L = 0; L < allString.Count(); L++)
            {
                if (count == gap)
                {
                    sw_test.WriteLine(allString[L]);
                    count = 1;
                }
                else
                {
                    sw_train.WriteLine(allString[L]);
                }
                count++;
            }

            sw_train.Flush();
            sw_train.Close();
            sw_test.Flush();
            sw_test.Close();
        }
        #endregion \\testing file//


    }
    public class LabellingInfo
    {
        public LabellingInfo(int index, Rectangle rect)
        {
            Index = index;
            boundingbox = rect;
        }
        public Rectangle boundingbox = new Rectangle();
        public int Index = -1;
    }

    public static class BitmapSourceConvert
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        public static BitmapSource MatToBitmap(IInputArray mat)
        {
            WriteableBitmap bitmap = null;
            if (((Mat)mat).NumberOfChannels == 4)
                bitmap = new WriteableBitmap(((Mat)mat).Width, ((Mat)mat).Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgra32, null);
            else if (((Mat)mat).NumberOfChannels == 3)
                bitmap = new WriteableBitmap(((Mat)mat).Width, ((Mat)mat).Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr24, null);
            else if (((Mat)mat).NumberOfChannels == 1)
                bitmap = new WriteableBitmap(((Mat)mat).Width, ((Mat)mat).Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Gray8, null);

            bitmap.Lock();
            unsafe
            {
                var region = new Int32Rect(0, 0, ((Mat)mat).Width, ((Mat)mat).Height);
                int ch = ((Mat)mat).NumberOfChannels;
                int stride = (((Mat)mat).Width * ch);
                int bitPerPixCh = 8;
                bitmap.WritePixels(region, ((Mat)mat).DataPointer, (stride * ((Mat)mat).Height), stride);
                bitmap.AddDirtyRect(region);
            }
            bitmap.Unlock();

            return bitmap;
        }
        //public static BitmapSource ToBitmapSource(IImage image)
        //{

        //    using (System.Drawing.Bitmap source = image.Bitmap)
        //    {
        //        IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

        //        BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
        //            ptr,
        //            IntPtr.Zero,
        //            Int32Rect.Empty,
        //            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

        //        DeleteObject(ptr); //release the HBitmap
        //        return bs;
        //    }
        //}
    }

    public class ListViewData : INotifyPropertyChanged
    {
        string pre = "";
        string folder;
        public string realPath;
        int count;
        public ListViewData(string Folder, string RealPath, int Count, SolidColorBrush C1 = null, SolidColorBrush C2 = null, SolidColorBrush C3 = null)
        {
            realPath = RealPath;
            folder = Folder;
            count = Count;
            if (C1 == null)
                Color1 = new SolidColorBrush(Colors.Black);
        }

        public string Pre
        {
            set
            {
                pre = value;
                NotifyPropertyChanged("Pre");
            }
            get { return pre; }
        }
        public string Folder
        {
            set
            {
                folder = value;
                NotifyPropertyChanged("Folder");
            }
            get { return folder; }
        }
        public int Count
        {
            set
            {
                count = value;
                NotifyPropertyChanged("Count");
            }
            get { return count; }
        }
        public SolidColorBrush Color_back
        {
            set
            {
                color_back = value;
                NotifyPropertyChanged("Color_back");
            }
            get { return color_back; }
        }


        SolidColorBrush color_back = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush Color1 { get; set; } = new SolidColorBrush(Colors.Black);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }
    }

    public static class Ex
    {
        public static double ToDouble(this string str)
        {
            return double.Parse(str);
        }
        public static int ToInt(this string str)
        {
            return int.Parse(str);
        }
        public static void AddLayer(this IInputOutputArray baseImg, Mat upLayer)
        {
            CvInvoke.Imwrite($"baseImg.png", baseImg);
            CvInvoke.Imwrite($"uplayer.png", upLayer);
            Mat mask = new Mat(((Mat)upLayer).Size, DepthType.Cv8U, 1);
            CvInvoke.CvtColor(upLayer, mask, ColorConversion.Bgr2Gray);
            CvInvoke.Imwrite($"mask.png", mask);
            CvInvoke.Threshold(mask, mask, 0, 255, ThresholdType.Binary);
            CvInvoke.Imwrite($"thres.png", mask);
            ((Mat)upLayer).CopyTo(baseImg, mask);
            CvInvoke.Imwrite($"out.png", baseImg);
        }
    }

}
