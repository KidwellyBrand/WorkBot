using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QRScannerAdd
{
    public partial class MainRibbon
    {
        private void MainRibbon_Load(object sender, RibbonUIEventArgs e) { }
        private void Scan_Click(object sender, RibbonControlEventArgs e)
        {
            var excelApp = Globals.ThisAddIn.Application;

            // Ввод QR-кода через InputBox Excel
            string qrInput = excelApp.InputBox("Введите QR-код", Type: 2) as string;

            if (string.IsNullOrWhiteSpace(qrInput))
            {
                MessageBox.Show("QR-код не введён.");
                return;
            }

            string qr = qrInput.Trim();

            Microsoft.Office.Interop.Excel.Worksheet sheet = excelApp.ActiveSheet;
            Microsoft.Office.Interop.Excel.Range usedRange = sheet.UsedRange;

            string foundId = null;
            string foundName = null;

            for (int row = 2; row <= usedRange.Rows.Count; row++)
            {
                // Чтение значения любого типа и конвертация в строку
                var cellObj = (usedRange.Cells[row, 1] as Microsoft.Office.Interop.Excel.Range)?.Value2;
                string cellQR = cellObj?.ToString().Trim();

                if (cellQR == qr)
                {
                    foundName = (usedRange.Cells[row, 2] as Microsoft.Office.Interop.Excel.Range)?.Value2?.ToString();
                    foundId = (usedRange.Cells[row, 3] as Microsoft.Office.Interop.Excel.Range)?.Value2?.ToString();
                    break;
                }
            }
            if (foundId == null)
            {
                MessageBox.Show("QR-код не найден.");
                return;
            }

            MessageBox.Show($"Найден пользователь: {foundName} с ID: {foundId}");

            string secondFilePath = @"C:\Users\Kidwelly\Documents\Книга2.xlsx";

            Microsoft.Office.Interop.Excel.Workbook secondWorkbook = null;
            Microsoft.Office.Interop.Excel.Worksheet secondSheet = null;

            try
            {
                bool isWorkbookOpen = false;
                foreach (Microsoft.Office.Interop.Excel.Workbook wb in excelApp.Workbooks)
                {
                    if (wb.FullName == secondFilePath)
                    {
                        secondWorkbook = wb;
                        isWorkbookOpen = true;
                        break;
                    }
                }

                if (!isWorkbookOpen)
                    secondWorkbook = excelApp.Workbooks.Open(secondFilePath);

                secondSheet = (Microsoft.Office.Interop.Excel.Worksheet)secondWorkbook.Sheets[1];

                // Найти первую пустую ячейку в первом столбце
                int insertRow = 1;
                while (true)
                {
                    var cellValue = (secondSheet.Cells[insertRow, 1] as Microsoft.Office.Interop.Excel.Range)?.Value2;
                    if (cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
                    {
                        break;
                    }
                    insertRow++;
                }

                // Вставить ID
                secondSheet.Cells[insertRow, 1].Value2 = foundId;

                secondWorkbook.Save();

                MessageBox.Show($"Найден ID: {foundId} успешно добавлен во второй файл в строку {insertRow}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при работе со вторым файлом: " + ex.Message);
            }
        }

    }
}




