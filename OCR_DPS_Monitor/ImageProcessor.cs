using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class ImageProcessor
{

    public Bitmap RemoveAreasByColor(Bitmap original, Color targetColor, int tolerance = 10)
    {
        // Находим координаты первого пикселя с нужным цветом
        Point? firstMatchPoint = FindColorPosition(original, targetColor, tolerance);

        if (firstMatchPoint.HasValue)
        {
            int startX = firstMatchPoint.Value.X;
            int startY = firstMatchPoint.Value.Y - 8;
            int endY = 120;

            // Создаем список областей для сохранения (все кроме удаляемых)
            var areasToKeep = new List<Rectangle>();

            // Область между первой и второй удаляемыми зонами
            int middleStartX = startX + 45;
            int middleEndX = startX + 285;
            if (middleStartX < middleEndX)
            {
                areasToKeep.Add(new Rectangle(middleStartX, startY, middleEndX - middleStartX, endY));
            }

            // Область справа от второй удаляемой зоны
            int rightStartX = startX + 285 + 61;
            if (rightStartX < original.Width)
            {
                areasToKeep.Add(new Rectangle(rightStartX, startY, original.Width - rightStartX, endY));
            }

            // Собираем все сохраняемые области вместе
            if (areasToKeep.Count > 0)
            {
                return CombineImageAreas(original, areasToKeep);
            }
            else
            {
                // Если нечего сохранять, возвращаем оригинал
                return original;
            }
        }
        else
        {
            // Если цвет не найден, возвращаем оригинальное изображение
            return original;
        }
    }

    private Bitmap CombineImageAreas(Bitmap source, List<Rectangle> areas)
    {
        // Вычисляем общую ширину всех сохраняемых областей
        int totalWidth = areas.Sum(area => area.Width);
        int maxHeight = areas.Max(area => area.Height);

        var result = new Bitmap(totalWidth, maxHeight);

        using (var g = Graphics.FromImage(result))
        {
            int currentX = 0;
            foreach (var area in areas)
            {
                // Копируем только нужную область из исходного изображения
                g.DrawImage(source,
                           new Rectangle(currentX, 0, area.Width, area.Height),
                           area,
                           GraphicsUnit.Pixel);
                currentX += area.Width;
            }
        }

        return result;
    }


    private Point? FindColorPosition(Bitmap image, Color targetColor, int tolerance)
    {
        // Проходим по всем пикселям изображения
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixel = image.GetPixel(x, y);
                if (IsColorMatch(pixel, targetColor, tolerance))
                {
                    return new Point(x, y);
                }
            }
        }
        return null;
    }

    private bool IsColorMatch(Color color1, Color color2, int tolerance)
    {
        return Math.Abs(color1.R - color2.R) <= tolerance &&
               Math.Abs(color1.G - color2.G) <= tolerance &&
               Math.Abs(color1.B - color2.B) <= tolerance;
    }

    public int findCharacterPosition(int amount, string sourcePath, Color targetColor, int tolerance = 10)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"File not found: {sourcePath}");
        }

        using (var image = new Bitmap(sourcePath))
        {
            // Находим позицию цвета обрамления
            Point? colorPoint = FindColorPosition(image, targetColor, tolerance);

            if (!colorPoint.HasValue)
            {
                return -1; // Цвет не найден
            }

            int x = colorPoint.Value.X;
            int y = colorPoint.Value.Y;

            if (amount == 4)
            {
                // Для 4 персонажей - только по Y координате
                if (y < 10) return 1;
                if (y >= 27 && y <= 31) return 2;
                if (y >= 57 && y <= 60) return 3;
                return 4;
            }
            else if (amount == 8)
            {
                // Для 8 персонажей - по X и Y координатам
                if (y < 10)
                {
                    return x < 100 ? 1 : 2;
                }
                else if (y >= 27 && y <= 31)
                {
                    return x < 100 ? 3 : 4;
                }
                else if (y >= 57 && y <= 60)
                {
                    return x < 100 ? 5 : 6;
                }
                else
                {
                    return x < 100 ? 7 : 8;
                }
            }
            else
            {
                throw new ArgumentException("Amount must be either 4 or 8", nameof(amount));
            }
        }
    }

}




