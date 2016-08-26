﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PassPrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static DirectoryInfo PDFDirectory
        {
            get
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

                while (directoryInfo != null && !directoryInfo.GetDirectories("PDFs").Any())
                {
                    directoryInfo = directoryInfo.Parent;
                }

                return directoryInfo?.GetDirectories("PDFs").FirstOrDefault();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            RenamePDFs();
            txtInput.Focus();
        }

        private void RenamePDFs()
        {
            FileInfo[] files = PDFDirectory?.GetFiles("*.pdf");

            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    RenamePDF(file);
                }
            }
        }

        private void RenamePDF(FileInfo file)
        {
            string attendeeName = GetAttendeeName(file.FullName);

            if (!string.IsNullOrWhiteSpace(attendeeName)
                && file.Name.Length == PassFile.GuidLength + PassFile.ExtensionLength)
            {
                string newFileName = $"{file.DirectoryName}\\{attendeeName} {file.Name}";
                File.Move(file.FullName, newFileName);
            }
        }

        public string GetAttendeeName(string fileName)
        {
            string text = string.Empty;

            if (File.Exists(fileName))
            {
                PdfReader pdfReader = new PdfReader(fileName);

                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                text = PdfTextExtractor.GetTextFromPage(pdfReader, 1, strategy);

                text = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text)));

                pdfReader.Close();

                if (text.Contains("\n"))
                {
                    text = text.Substring(0, text.IndexOf("\n")).Trim();
                }
            }

            return text;
        }

        public void Search(string input)
        {
            FileInfo[] files = PDFDirectory?.GetFiles($"*{input}*.pdf");

            if (files != null)
            {
                List<PassFile> passFiles = files.Select(f => new PassFile(f.Name)).ToList();
                grdPDFs.ItemsSource = passFiles;

                if (passFiles.Count == 1)
                {
                    PreviewPDF(passFiles.First());
                }
                else
                {
                    PDFPreview.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void btnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            Search(txtInput.Text);
        }

        private void txtInput_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtInput.Text.Trim().Length >= 3)
            {
                Search(txtInput.Text);
            }
        }

        private void btnClear_OnClick(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            grdPDFs.ItemsSource = new List<PassFile>();
            PDFPreview.Visibility = Visibility.Collapsed;
            txtInput.Clear();
            txtInput.Focus();
        }

        private void btnPreviewPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            PreviewPDF(file);
        }

        private void PreviewPDF(PassFile file)
        {
            MainWindowStackPanel.IsEnabled = false;
            string fileName = $"{PDFDirectory.FullName}\\{file.FileName}";

            Uri url = new Uri($"file:///{fileName}", UriKind.Absolute);
            PDFPreview.Navigate(url);
            PDFPreview.Visibility = Visibility.Visible;
        }

        private void btnOpenPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            OpenPDF(file);
            Clear();
        }

        private void OpenPDF(PassFile file)
        {
            string fileName = $"{PDFDirectory.FullName}\\{file.FileName}";

            Process process = new Process
            {
                StartInfo = { FileName = fileName }
            };

            process.Start();
            process.WaitForExit();
            txtInput.Focus();
        }

        private void btnPrintPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            PrintPDF(file);
            Clear();
        }

        private static void PrintPDF(PassFile file)
        {
            string fileName = $"{PDFDirectory.FullName}\\{file.FileName}";

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    Verb = "print",
                    FileName = fileName
                }
            };

            process.Start();
        }

        private void PDFPreview_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            MainWindowStackPanel.IsEnabled = true;
        }
    }
}
