using System.Diagnostics;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Windows.Automation.Peers;

namespace AdMirai
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "AdMirai";
        }

        private void Seleccionar_Click(object sender, RoutedEventArgs e)
        {
            //Console.Write($"Seleccionar_Click()");
            Trace.WriteLine("Seleccionar_Click()");

            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".jpg"; // Default file extension
            dialog.Filter = "Imágenes (.jpg)|*.jpg"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                textBoxResult.Text = "";
                
                Task.Run(() =>
                {
                    // Open document
                    string filename = dialog.FileName;
                    //Console.Write($"filename {filename}");
                    Trace.WriteLine($"filename {filename}");

                    // path for model and images
                    var modelPath = @"C:\AdMirai\phi3_onnx";
                    var img = Images.Load(filename);

                    // define prompts
                    var systemPrompt = "You are an AI assistant that helps people find information. Answer questions using a direct style. Do not share more information that the requested by the users.";
                    string userPrompt = "Describe la imagen en idioma español";
                    var fullPrompt = $"<|system|>{systemPrompt}<|end|><|user|><|image_1|>{userPrompt}<|end|><|assistant|>";

                    // load model and create processor
                    using Model model = new Model(modelPath);
                    using MultiModalProcessor processor = new MultiModalProcessor(model);
                    using var tokenizerStream = processor.CreateStream();

                    // create the input tensor with the prompt and image
                    Console.WriteLine("Reading the image...");
                    var inputTensors = processor.ProcessImages(fullPrompt, img);
                    using GeneratorParams generatorParams = new GeneratorParams(model);
                    generatorParams.SetSearchOption("max_length", 3072);
                    generatorParams.SetInputs(inputTensors);

                    // generate response
                    using var generator = new Generator(model, generatorParams);

                    while (!generator.IsDone())
                    {
                        generator.ComputeLogits();
                        generator.GenerateNextToken();
                        var seq = generator.GetSequence(0)[^1];
                        var decodedResult = tokenizerStream.Decode(seq);
                        Trace.Write(decodedResult);
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
                         new Action(() =>
                         {
                             textBoxResult.Text = textBoxResult.Text + decodedResult;
                         }
                        ));
                    } // while

 
                    SystemSounds.Beep.Play();
                });
            } //if


        } // private void

    } // public

} // namespace