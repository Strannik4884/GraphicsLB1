using System;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;

namespace LB1
{
    public partial class MainWindow : Window
    {
        private bool IsSavedImage = true;
        private delegate byte[] Filter(byte[] rgbValues);

        public MainWindow()
        {
            InitializeComponent();
        }

        // обработчик клика на пункт меню нового файла
        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSavedImage)
            {
                if (MessageBox.Show("Текущее изображение не сохранено! Удалить его и продолжить?", "Несохранённые данные", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    ImageBox.Source = null;
                    UpdateUI();
                }
            }
            else
            {
                ImageBox.Source = null;
                UpdateUI();
            }
        }

        // обработчик клика на пункт меню открытия файла
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsSavedImage)
                {
                    if (MessageBox.Show("Текущее изображение не сохранено! Удалить его и продолжить?", "Несохранённые данные", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog
                        {
                            Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp"
                        };
                        if (openFileDialog.ShowDialog() == true)
                        {
                            ImageBox.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                            UpdateUI();
                        }
                    }
                }
                else
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp"
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        ImageBox.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                        IsSavedImage = true;
                        UpdateUI();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка при открытии изображения: неверный формат файла", "Ошибка файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // обработчик клика на пункт меню сохранения файла
        private void SaveFileAs_Click(object sender, RoutedEventArgs e)
        {

        }

        // обработчик клика на пункт меню закрытия программы
        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // обработчик события закрытия приложения
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsSavedImage)
            {
                if (MessageBox.Show("Текущее изображение не сохранено! Удалить его и продолжить?", "Несохранённые данные", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        // обработчик перетаскивания файла изображения на окно приложения
        private void ImageBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!IsSavedImage)
                {
                    if (MessageBox.Show("Текущее изображение не сохранено! Удалить его и продолжить?", "Несохранённые данные", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                        {
                            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                            ImageBox.Source = new BitmapImage(new Uri(files[0]));
                            UpdateUI();
                        }
                    }
                }
                else
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        ImageBox.Source = new BitmapImage(new Uri(files[0]));
                        UpdateUI();
                    }
                }
            }
            catch(Exception)
            {
                MessageBox.Show("Ошибка при открытии изображения: неверный формат файла", "Ошибка файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // функция для обновления элементов пользовательского интерфейса
        private void UpdateUI()
        {
            IsSavedImage = true;
            if (ImageBox.Source != null)
            {
                SaveFileAs.IsEnabled = true;
                Filters.IsEnabled = true;
                EmptyImageBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                SaveFileAs.IsEnabled = false;
                Filters.IsEnabled = false;
                EmptyImageBox.Visibility = Visibility.Visible;
            }
        }

        // функция для работы с Bitmap'ом напрямую
        private void MakeFilter(Filter filter)
        {
            try
            {
                // получаем Bitmap текущего изображения
                Bitmap bmp = GetBitmap((BitmapSource)ImageBox.Source);

                // блокируем данные Bitmap'а
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

                // получаем указатель на начало Bitmap'а
                IntPtr ptr = bmpData.Scan0;

                // объявляем массив для хранения пикселей
                int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
                byte[] rgbValues = new byte[bytes];

                // копируем пиксели в массив
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                // накладываем фильтр
                rgbValues = filter(rgbValues);

                // копируем значения пикселей обратно в Bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

                // разблокируем данные Bitmap'а
                bmp.UnlockBits(bmpData);

                // обновляем изображение на форме
                ImageBox.Source = Convert(bmp);
                IsSavedImage = false;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка при наложении фильтра: " + ex.Message, "Ошибка фильтра", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // функция наложения чёрно-белого фильтра
        private byte[] MakeBlackWhiteFilter(byte[] rgbValues)
        {
            // проходимся по всем пикселям
            for (int i = 0; i < rgbValues.Length; i += 4)
            {
                // получаем среднее значение для цветов RGB
                int averageValue = (rgbValues[i] + rgbValues[i + 1] + rgbValues[i + 2]) / 3;
                // устанавливаем среднее значение каждому цвету RGB
                rgbValues[i] = rgbValues[i + 1] = rgbValues[i + 2] = (byte)averageValue;
            }

            return rgbValues;
        }

        // функция наложения негатива
        private byte[] MakeNegativeFilter(byte[] rgbValues)
        {
            // проходимся по всем пикселям
            for (int i = 0; i < rgbValues.Length; i += 4)
            {
                // получаем текущие значения ARGB
                int R = rgbValues[i];
                int G = rgbValues[i + 1];
                int B = rgbValues[i + 2];
                int A = rgbValues[i + 3];
                // меняем местами значения ARGB
                rgbValues[i] = (byte)(255 - R);
                rgbValues[i + 1] = (byte)(255 - G);
                rgbValues[i + 2] = (byte)(255 - B);
                rgbValues[i + 3] = (byte)(255 - A);
            }

            return rgbValues;
        }

        // функция наложения шума
        private byte[] MakeNoiseFilter(byte[] rgbValues)
        {
            Random random = new Random();
            // проходимся по всем пикселям
            for (int i = 0; i < rgbValues.Length; i += 4)
            {
                // с вероятностью 60% устанавливаем серый цвет текущему пикселю
                int q = random.Next(100);
                if (q <= 40)
                {
                    rgbValues[i] = Color.Gray.R;
                    rgbValues[i + 1] = Color.Gray.G;
                    rgbValues[i + 2] = Color.Gray.B;
                }
            }

            return rgbValues;
        }

        // функция наложения сепии
        private byte[] MakeSepiaFilter(byte[] rgbValues)
        {
            // получаем негатив изображения
            rgbValues = MakeNegativeFilter(rgbValues);

            // проходимся по всем пикселям
            for (int i = 0; i < rgbValues.Length; i += 4)
            {
                // получаем текущее значение RGB
                int R = rgbValues[i];
                int G = rgbValues[i + 1];
                int B = rgbValues[i + 2];
                // определяем новые значения для RGB сепия
                int tr = (int)(0.393 * R + 0.769 * G + 0.189 * B);
                int tg = (int)(0.349 * R + 0.686 * G + 0.168 * B);
                int tb = (int)(0.272 * R + 0.534 * G + 0.131 * B);
                // устанавливаем новые значения RGB учитывая переполнение
                rgbValues[i] = (byte)(tr > 255 ? 255 : tr);
                rgbValues[i + 1] = (byte)(tg > 255 ? 255 : tg);
                rgbValues[i + 2] = (byte)(tb > 255 ? 255 : tb);
            }

            // убираем негатив и возвращаем полученный массив
            return MakeNegativeFilter(rgbValues);
        }

        // обработчик клика на пункт меню чёрно-белого шума
        private void BlackWhiteFilter_Click(object sender, RoutedEventArgs e)
        {
            MakeFilter(MakeBlackWhiteFilter);
        }

        // обработчик клика на пункт меню негатива
        private void NegativeFilter_Click(object sender, RoutedEventArgs e)
        {
            MakeFilter(MakeNegativeFilter);
        }

        // обработчик клика на пункт меню шума
        private void NoiseFilter_Click(object sender, RoutedEventArgs e)
        {
            MakeFilter(MakeNoiseFilter);
        }

        // обработчик клика на пункт меню сепии
        private void SepiaFilter_Click(object sender, RoutedEventArgs e)
        {
            MakeFilter(MakeSepiaFilter);
        }

        // функция для перевода BitmapSource в Bitmap
        private Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);

            return bmp;
        }

        // функция для перевода Bitmap в BitmapSource
        private BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }
    }
}
