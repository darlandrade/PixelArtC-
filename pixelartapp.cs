using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;



namespace PixelArt
{    
    public partial class Form1 : Form
    {
        private Label lblFrameIndicator;

        readonly Color FUNDOJANELA = Color.FromArgb(60,60,60);
        readonly Color FUNDOBTN = Color.FromArgb(30,30,30);
        readonly Color BTNBORDER = Color.FromArgb(10,10,10);
        readonly Color MOUSEHOVERBTN = Color.FromArgb(22, 118, 120);

        private Panel panelFerramentas;
        private Panel panelCanvas;
        private Panel panelFuncoes;
        private Panel panelRodape;

        private PictureBox pictureBoxCanvas;

        private Bitmap canvasBitmap;
        private int pixelSize = 20;
        private bool isDrawing = false;
        private string currentTool = "Pencil"; // Lápis, Borracha, Balde
        // --- Variáveis para formas ---
        private Point? startPoint = null;   // ponto inicial do clique
        private Point currentPoint;         // posição atual do mouse

        private MouseButtons mouseButtonInUse; // Botão que iniciou o desenho
        private Color currentColor = Color.Black;
        private Color secondaryColor = Color.Transparent; // Cor do botão direito

        // Espelho
        private string mirrorMode = "None"; // "None", "H", "V", "HV"
        private Button btnMirrorH;
        private Button btnMirrorV;
        private Button btnMirrorHV;

        // Atalhos
        private Button btnPencil;
        private Button btnEraser;
        private Button btnBucket;
        private Button btnRectangle;
        private Button btnCircle;
        private Button btnLine;
        private Button btnReplaceColor;
        private Button btnColorPicker;


        // At the class level (top of your Form)
        private Dictionary<string, Button> toolButtons = new(); // Map tool names to buttons


        private List<Color> paletteColors = new List<Color>
        {
            Color.Black, Color.Red, Color.Green, Color.Blue, Color.Yellow
        };

        public Form1()
        {
            InitializeComponent();
            CriarLayout();
            InicializarCanvas();
            CriarFerramentas();
            CriarFuncoes();
            CriarRodape();
            ConfigurarAtalhos();

            this.Text = "Pixel Art Editor"; //  Título do formulário

            frames.Add(canvasBitmap);
            this.Icon = new Icon("pixelarticon.ico"); // Define o ícone do formulário)
            // Define que a janela será manualmente posicionada
            this.StartPosition = FormStartPosition.Manual;


            // Ajusta o tamanho inicial para 90% da tela e centraliza
            this.Load += (s, e) =>
            {
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                this.Width = (int)(screen.Width * 0.9);
                this.Height = (int)(screen.Height * 0.95);

                // Centraliza
                this.Left = (screen.Width - this.Width) / 2;
                this.Top = (screen.Height - this.Height) / 2;
            };

            panelCanvas.MouseWheel += PanelCanvas_MouseWheel;
            panelCanvas.Focus();
            panelCanvas.MouseEnter += (s, e) => panelCanvas.Focus();

        }
        private void PanelCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = 1.1f;
            if (e.Delta < 0) zoomFactor = 1 / zoomFactor;

            AjustarZoomFluido(zoomFactor);
        }

        private void AjustarZoomFluido(float factor)
        {
            // novo pixelSize como float
            float novoPixelSize = pixelSize * factor;

            // limites
            float minPixel = 4f;

            // calcula máximo baseado em 90% do painel
            int maxWidthPixel = (int)((panelCanvas.Width * 0.9) / canvasBitmap.Width);
            int maxHeightPixel = (int)((panelCanvas.Height * 0.9) / canvasBitmap.Height);
            float maxPixel = Math.Min(maxWidthPixel, maxHeightPixel);

            // garante que não ultrapasse limites
            if (novoPixelSize < minPixel) novoPixelSize = minPixel;
            if (novoPixelSize > maxPixel) novoPixelSize = maxPixel;

            // aplica somente depois de limitar
            pixelSize = (int)Math.Round(novoPixelSize);

            // atualiza tamanho do PictureBox
            pictureBoxCanvas.Width = canvasBitmap.Width * pixelSize;
            pictureBoxCanvas.Height = canvasBitmap.Height * pixelSize;

            // centraliza
            pictureBoxCanvas.Left = (panelCanvas.Width - pictureBoxCanvas.Width) / 2;
            pictureBoxCanvas.Top = (panelCanvas.Height - pictureBoxCanvas.Height) / 2;

            pictureBoxCanvas.Invalidate();
        }

        private void CriarLayout()
        {
            // Painel esquerdo - Ferramentas
            panelFerramentas = new Panel
            {
                Dock = DockStyle.Left,
                Width = 120,
                BackColor = FUNDOJANELA
            };
            Controls.Add(panelFerramentas);

            // Painel direito - Funções (paleta)
            panelFuncoes = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = FUNDOJANELA,
                Padding = new Padding(10),
                AutoScroll = true,
            };
            Controls.Add(panelFuncoes);

            // Painel inferior - Rodapé
            panelRodape = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = FUNDOJANELA,
                Padding = new Padding(10)
            };
            Controls.Add(panelRodape);

            // Painel central - Canvas
            panelCanvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.DarkGray, // dá contraste para ver o canvas
                AutoScroll = true
            };
            Controls.Add(panelCanvas);

            // PictureBox do Canvas
            pictureBoxCanvas = new PictureBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Normal
            };
            panelCanvas.Controls.Add(pictureBoxCanvas);

            // Ajusta o PictureBox quando o painel mudar de tamanho
            panelCanvas.Resize += (s, e) => CentralizarCanvas();
        }

        private void InicializarCanvas()
        {
            canvasBitmap = new Bitmap(32, 32);
            pictureBoxCanvas.Image = canvasBitmap;

            // Ajusta para não ocupar todo o painel, para podermos centralizar
            pictureBoxCanvas.SizeMode = PictureBoxSizeMode.Normal;
            pictureBoxCanvas.Width = canvasBitmap.Width * pixelSize;
            pictureBoxCanvas.Height = canvasBitmap.Height * pixelSize;

            // Centraliza dentro do painel
            pictureBoxCanvas.Left = (panelCanvas.Width - pictureBoxCanvas.Width) / 2;
            pictureBoxCanvas.Top = (panelCanvas.Height - pictureBoxCanvas.Height) / 2;

            // Faz o canvas reagir ao redimensionamento do painel
            panelCanvas.Resize += (s, e) =>
            {
                pictureBoxCanvas.Left = (panelCanvas.Width - pictureBoxCanvas.Width) / 2;
                pictureBoxCanvas.Top = (panelCanvas.Height - pictureBoxCanvas.Height) / 2;
            };

            pictureBoxCanvas.MouseDown += PictureBoxCanvas_MouseDown;
            pictureBoxCanvas.MouseMove += PictureBoxCanvas_MouseMove;
            pictureBoxCanvas.MouseUp += PictureBoxCanvas_MouseUp;
            pictureBoxCanvas.Paint += PictureBoxCanvas_Paint;
        }


        // Centraliza o PictureBox no painelCanvas
        private void CentralizarCanvas()
        {
            if (pictureBoxCanvas == null || panelCanvas == null) return;

            pictureBoxCanvas.Left = Math.Max((panelCanvas.Width - pictureBoxCanvas.Width) / 2, 0);
            pictureBoxCanvas.Top = Math.Max((panelCanvas.Height - pictureBoxCanvas.Height) / 2, 0);
        }
        private void CriarFerramentas()
        {
            var tools = new (string Text, string Tool)[]
            {
                ("Lápis", "Pencil"),
                ("Borracha", "Eraser"),
                ("Balde", "Bucket"),
                ("Retângulo", "Rectangle"),
                ("Círculo", "Circle"),
                ("Linha", "Line"),
                ("Trocar Cor", "ReplaceColor"),
                ("Conta-gotas", "ColorPicker")
            };

            int top = 10;
            foreach (var (text, tool) in tools)
            {
                var btn = new Button 
                { 
                    Text = text, 
                    Left = 10, 
                    Top = top, 
                    Width = 100, 
                    Height = 50,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    
                };
                // flat style
                btn.FlatAppearance.BorderColor = BTNBORDER;
                btn.Click += (s, e) => { currentTool = tool; SetActiveButton(btn); };

                panelFerramentas.Controls.Add(btn);

                top += 50;

                AddHoverEffect(btn);

                // save reference for shortcuts
                toolButtons[tool] = btn;
            }
            
            SetActiveButton((Button)panelFerramentas.Controls[0]);
            currentTool = "Pencil";


        }
        // Function to handle active button highlighting
        void SetActiveButton(Button activeBtn)
        {
            foreach (var ctrl in panelFerramentas.Controls)
            {
                if (ctrl is Button btn)
                    btn.BackColor = FUNDOBTN; // default button color
            }
            activeBtn.BackColor = Color.DarkCyan; // highlight current
        }

        private Panel panelLeftColor;
        private Panel panelRightColor;

        // --- Palette Area Refactored ---
        // Place this inside your Form1 class, replacing your current CriarFuncoes and AtualizarPaleta methods.
        // This version ensures proper toggling of remove mode, visual feedback, and correct button recreation.

        private Button btnRemoveColor; // Make this a field so you can update its state

        private void CriarFuncoes()
        {
            Label lblPaleta = new Label
            {
                Text = "Paleta de Cores",
                Left = 10,
                Top = 10,
                Width = 150,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            panelFuncoes.Controls.Add(lblPaleta);

            int btnSize = 30;
            int margem = 5;
            int inicioY = lblPaleta.Bottom + 15;

            // Create color buttons (will be recreated in AtualizarPaleta)
            AtualizarPaleta();

            // Botão "+"
            Button btnAddColor = new Button
            {
                Text = "+",
                Width = 30,
                Height = 30,
                Left = 10,
                Top = inicioY + 3 * (btnSize + margem) + 10,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN

            };
            btnAddColor.FlatAppearance.BorderSize = 1;
            btnAddColor.Click += (s, e) =>
            {
                if (paletteColors.Count < 15)
                {
                    using (ColorDialog cd = new ColorDialog())
                    {
                        if (cd.ShowDialog() == DialogResult.OK)
                        {
                            paletteColors.Add(cd.Color);
                            AtualizarPaleta();
                        }
                    }
                }
                else
                    MessageBox.Show("A paleta está cheia (máx. 15 cores).", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnAddColor.FlatAppearance.BorderSize = 1;
            btnAddColor.FlatAppearance.BorderColor = BTNBORDER;
            //btnAddColor.ForeColor = Color.FromArgb(45, 45, 45);

            btnAddColor.MouseEnter += (s, e) => // hover effect
            {
                if (btnAddColor.BackColor != Color.DarkCyan) // active button
                    btnAddColor.BackColor = MOUSEHOVERBTN;

            };

            btnAddColor.MouseLeave += (s, e) => // remove hover effect
            {
                if (btnAddColor.BackColor != Color.DarkCyan)
                    btnAddColor.BackColor = FUNDOBTN;
            };

            AddHoverEffect(btnAddColor);
            panelFuncoes.Controls.Add(btnAddColor);

            // Botão de remover cor (trash)
            btnRemoveColor = new Button
            {
                Width = 30,
                Height = 30,
                Left = btnAddColor.Right + 80,
                Top = btnAddColor.Top,
                Image = PixelArt.Properties.Resources.Trashbtn, // Add a trash icon to your resources
                FlatStyle = FlatStyle.Flat,
            };
            
            btnRemoveColor.FlatAppearance.BorderSize = 0; 
            btnRemoveColor.ImageAlign = ContentAlignment.MiddleCenter;
            btnRemoveColor.TextImageRelation = TextImageRelation.Overlay;
            btnRemoveColor.Text = "";
            btnRemoveColor.Click += (s, e) =>
            {
                removeColorMode = !removeColorMode;
                btnRemoveColor.BackColor = removeColorMode ? Color.DarkCyan : FUNDOJANELA;
                AtualizarPaleta();
            };

            btnRemoveColor.MouseEnter += (s, e) => // hover effect
            {
                if (btnRemoveColor.BackColor != Color.DarkCyan) // active button
                    btnRemoveColor.BackColor = MOUSEHOVERBTN;

            };

            btnRemoveColor.MouseLeave += (s, e) => // remove hover effect
            {
                if (btnRemoveColor.BackColor != Color.DarkCyan)
                    btnRemoveColor.BackColor = FUNDOJANELA;
            };

            panelFuncoes.Controls.Add(btnRemoveColor);

            // Painel indicador da cor do botão esquerdo
            panelLeftColor = new Panel
            {
                Width = 30,
                Height = 30,
                Left = btnAddColor.Right + 5,
                Top = btnAddColor.Top,
                BackColor = currentColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelFuncoes.Controls.Add(panelLeftColor);

            // Painel indicador da cor do botão direito

            panelRightColor = new Panel
            {
                Width = 30,
                Height = 30,
                Left = panelLeftColor.Right + 5,
                Top = btnAddColor.Top,
                BackColor = secondaryColor,
                BorderStyle = BorderStyle.FixedSingle,
              
            };
            panelFuncoes.Controls.Add(panelRightColor);

            int mirrorTop = btnAddColor.Bottom + 10;

            Button MakeBtn(string text, int left, int width = 30) => new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                Left = left,
                Top = mirrorTop,
                BackColor = FUNDOBTN,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };

            btnMirrorH = MakeBtn("H", 10);
            btnMirrorV = MakeBtn("V", btnMirrorH.Right + 5);
            btnMirrorHV = MakeBtn("H+V", btnMirrorV.Right + 5, 45);

            btnMirrorH.FlatAppearance.BorderSize = 1;
            btnMirrorV.FlatAppearance.BorderSize = 1;
            btnMirrorHV.FlatAppearance.BorderSize = 1;

            btnMirrorH.FlatAppearance.BorderColor = BTNBORDER;
            btnMirrorV.FlatAppearance.BorderColor = BTNBORDER;
            btnMirrorHV.FlatAppearance.BorderColor = BTNBORDER;

            btnMirrorV.Click += (s, e) => AtivarEspelho("V"); 
            btnMirrorH.Click += (s, e) => AtivarEspelho("H");
            btnMirrorHV.Click += (s, e) => AtivarEspelho("HV");

            panelFuncoes.Controls.Add(btnMirrorH);
            panelFuncoes.Controls.Add(btnMirrorV);
            panelFuncoes.Controls.Add(btnMirrorHV);

            AddHoverEffect(btnMirrorH);
            AddHoverEffect(btnMirrorV);
            AddHoverEffect(btnMirrorHV);
        }

        void AddHoverEffect(Button btn)
        {
            btn.MouseEnter += (s, e) =>
            {
                if (btn.BackColor != Color.DarkCyan) // your active color
                    btn.BackColor = MOUSEHOVERBTN;
            };

            btn.MouseLeave += (s, e) =>
            {
                if (btn.BackColor != Color.DarkCyan)
                    btn.BackColor = FUNDOBTN;
            };
        }

        private void AtualizarPaleta()
        {
            // Remove only color buttons (not "+", trash, indicators, or mirror buttons)
            for (int i = panelFuncoes.Controls.Count - 1; i >= 0; i--)
            {
                var c = panelFuncoes.Controls[i];
                if (c is Button btn && btn.Tag is string tag && tag == "palette")
                    panelFuncoes.Controls.RemoveAt(i);
            }

            int btnSize = 30;
            int margem = 5;
            int inicioY = 55; // adjust as needed

            for (int i = 0; i < 15; i++)
            {
                Button colorBtn = new Button
                {
                    Width = btnSize,
                    Height = btnSize,
                    Left = 10 + (i % 5) * (btnSize + margem),
                    Top = inicioY + (i / 5) * (btnSize + margem),
                    BackColor = i < paletteColors.Count ? paletteColors[i] : Color.Transparent,
                    Enabled = i < paletteColors.Count,
                    FlatStyle = FlatStyle.Flat,
                    Text = "",
                    Tag = "palette", // Mark for easy removal                                     
                };

                colorBtn.Tag = "palette";
                colorBtn.AccessibleDescription = i.ToString(); // Store index for removal
                colorBtn.FlatAppearance.BorderSize = 1;
                colorBtn.ForeColor = Color.FromArgb(45,45,45);

                colorBtn.Paint += (s, e) =>
                {
                    if (removeColorMode && colorBtn.Enabled)
                    {
                        var g = e.Graphics;
                        g.DrawLine(Pens.Black, 5, 5, btnSize - 5, btnSize - 5); // \
                        g.DrawLine(Pens.Black, btnSize - 5, 5, 5, btnSize - 5); // X
                    }
                };

                colorBtn.MouseDown += (s, e) =>
                {
                    if (!colorBtn.Enabled) return;
                    int idx = int.Parse(colorBtn.AccessibleDescription);
                    if (removeColorMode)
                    {
                        if (idx < paletteColors.Count)
                        {
                            paletteColors.RemoveAt(idx);
                            AtualizarPaleta();
                        }
                    }
                    else
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            currentColor = colorBtn.BackColor;
                            AtualizarIndicadoresDeCor();
                        }
                        else if (e.Button == MouseButtons.Right)
                        {
                            secondaryColor = colorBtn.BackColor;
                            AtualizarIndicadoresDeCor();
                        }
                    }
                };

                panelFuncoes.Controls.Add(colorBtn);
            }
        }

        private void CriarRodape()
        {
            Button btnExportar = new Button
            {
                Text = "Exportar PNG",
                Width = 120,
                Height = 30,
                Left = 10,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN,
                FlatAppearance = { BorderColor = BTNBORDER, BorderSize = 1 }
            };
            btnExportar.Click += (s, e) => ExportarPNG();
            panelRodape.Controls.Add(btnExportar);

            Button btnPrevFrame = new Button { Text = "<", Width = 40, Height = 30, Left = 140, Top = 10,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN,
                FlatAppearance = { BorderColor = BTNBORDER, BorderSize = 1 }
            };
            btnPrevFrame.Click += (s, e) => TrocarFrame(currentFrameIndex - 1);
            panelRodape.Controls.Add(btnPrevFrame);

            Label lblFrameIndicator = new Label
            {
                Width = 80,
                Height = 30,
                Left = 180, // position it between Prev and Next frame buttons
                Top = 10,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 45), // optional dark background
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Text = $"Frame: {currentFrameIndex + 1}/{frames.Count}"
            };

            panelRodape.Controls.Add(lblFrameIndicator);


            Button btnNextFrame = new Button { Text = ">", Width = 40, Height = 30, Left = 260, Top = 10,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN,
                FlatAppearance = { BorderColor = BTNBORDER, BorderSize = 1 }
            };
            btnNextFrame.Click += (s, e) => TrocarFrame(currentFrameIndex + 1);
            panelRodape.Controls.Add(btnNextFrame);

            Button btnAddFrame = new Button { Text = "+ Frame", Width = 80, Height = 30, Left = 310, Top = 10,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN,
                FlatAppearance = { BorderColor = BTNBORDER, BorderSize = 1 }
            };
            btnAddFrame.Click += (s, e) => AdicionarFrame();
            panelRodape.Controls.Add(btnAddFrame);

            Button btnDeleteFrame = new Button { Text = "- Frame", Width = 80, Height = 30, Left = 400, Top = 10 ,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN,
                FlatAppearance = { BorderColor = BTNBORDER, BorderSize = 1 }
            };
            btnDeleteFrame.Click += (s, e) => RemoverFrame();
            panelRodape.Controls.Add(btnDeleteFrame);

            Button btnStartOver = new Button { Text = "Recomeçar", Width = 100, Height = 30, Left = 800, Top = 10 ,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = FUNDOBTN,
                FlatAppearance = { BorderColor = BTNBORDER, BorderSize = 1 }
            };
            btnStartOver.Click += (s, e) => 
            {
                if (MessageBox.Show("Tem certeza que deseja recomeçar? Isso apagará todo o progresso.", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    frames.Clear();
                    currentFrameIndex = 0;
                    InicializarCanvas();
                    frames.Add(canvasBitmap);
                }
            };
            panelRodape.Controls.Add(btnStartOver);

            panelRodape.Controls.Add(lblFrameIndicator);

            AddHoverEffect(btnExportar);
            AddHoverEffect(btnPrevFrame);
            AddHoverEffect(btnNextFrame);
            AddHoverEffect(btnAddFrame);
            AddHoverEffect(btnDeleteFrame);
            AddHoverEffect(btnStartOver);
        }

        private void TrocarFrame(int newIndex)
        {
            if (newIndex < 0 || newIndex >= frames.Count) return;
            currentFrameIndex = newIndex;
            canvasBitmap = frames[currentFrameIndex];
            pictureBoxCanvas.Image = canvasBitmap;
            pictureBoxCanvas.Width = canvasBitmap.Width * pixelSize;
            pictureBoxCanvas.Height = canvasBitmap.Height * pixelSize;
            pictureBoxCanvas.Invalidate();
            // Safely update frame indicator
            if (lblFrameIndicator != null)
                lblFrameIndicator.Text = $"Frame: {currentFrameIndex + 1}/{frames.Count}";
        }

        private void AdicionarFrame()
        {
            Bitmap newFrame = new Bitmap(canvasBitmap.Width, canvasBitmap.Height);
            using (Graphics g = Graphics.FromImage(newFrame))
                g.Clear(Color.Transparent);
            frames.Insert(currentFrameIndex + 1, newFrame);
            TrocarFrame(currentFrameIndex + 1);
        }

        private void RemoverFrame()
        {
            if (frames.Count <= 1) return;
            frames.RemoveAt(currentFrameIndex);
            if (currentFrameIndex >= frames.Count)
                currentFrameIndex = frames.Count - 1;

            TrocarFrame(currentFrameIndex); // will update canvas and label
        }

        private void ExportarPNG()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png";
                if (sfd.ShowDialog() == DialogResult.OK)
                    canvasBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void PictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            int x = e.X / pixelSize;
            int y = e.Y / pixelSize;

            if (x < 0 || y < 0 || x >= canvasBitmap.Width || y >= canvasBitmap.Height)
                return;

            mouseButtonInUse = e.Button;
            isDrawing = true;

            IniciarAcao();

            if (currentTool == "Rectangle" || currentTool == "Circle" || currentTool == "Line")
            {
                startPoint = new Point(x, y);
                currentPoint = startPoint.Value;
            }
            else
            {
                lastPreviewPoint = new Point(x, y);
                currentPoint = new Point(x, y);
                AplicarFerramenta(e.X, e.Y, e.Button);
                pictureBoxCanvas.Invalidate(); // Ensure immediate redraw
            }
        }

        private void PictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            int x = e.X / pixelSize;
            int y = e.Y / pixelSize;

            if (!isDrawing) return;
            if (x < 0 || y < 0 || x >= canvasBitmap.Width || y >= canvasBitmap.Height)
                return;

            if (currentTool == "Rectangle" || currentTool == "Circle" || currentTool == "Line")
            {
                currentPoint = new Point(x, y);
                pictureBoxCanvas.Invalidate();
            }
            else
            {
                // For Pencil/Eraser, update preview line
                currentPoint = new Point(x, y);
                pictureBoxCanvas.Invalidate();
                AplicarFerramenta(e.X, e.Y, mouseButtonInUse);
                lastPreviewPoint = currentPoint;
            }
        }

        private void PictureBoxCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;

            int x = e.X / pixelSize;
            int y = e.Y / pixelSize;

            if (startPoint != null && (currentTool == "Rectangle" || currentTool == "Circle" || currentTool == "Line"))
            {
                DesenharFormaFinal(startPoint.Value, new Point(x, y));
                startPoint = null;
            }

            isDrawing = false;
            lastPreviewPoint = null;
            FinalizarAcao();
            pictureBoxCanvas.Invalidate();
        }

        private void AplicarFerramenta(int mouseX, int mouseY, MouseButtons botao)
        {
            int x = mouseX / pixelSize;
            int y = mouseY / pixelSize;

            if (x < 0 || x >= canvasBitmap.Width || y < 0 || y >= canvasBitmap.Height)
                return;

            Color corUsar = botao == MouseButtons.Left ? currentColor : secondaryColor;

            switch (currentTool)
            {
                case "Pencil":
                    DesenharComEspelho(x, y, corUsar);
                    break;
                case "Eraser":
                    DesenharComEspelho(x, y, Color.Transparent);
                    break;
                case "Bucket":
                    FloodFill(x, y, canvasBitmap.GetPixel(x, y), corUsar);
                    break;
                case "ReplaceColor":
                    ReplaceAllPixelsOfColor(canvasBitmap.GetPixel(x, y), corUsar);
                    break;
                case "ColorPicker":
                    PickColorFromCanvas(x, y, botao);
                    break;
            }

            pictureBoxCanvas.Invalidate(new Rectangle(x * pixelSize, y * pixelSize, pixelSize, pixelSize));

            void DesenharComEspelho(int px, int py, Color cor)
            {
                // Use RegistrarMudancaPixel for all affected pixels
                RegistrarMudancaPixel(px, py, cor);

                int w = canvasBitmap.Width;
                int h = canvasBitmap.Height;

                if (mirrorMode == "H" || mirrorMode == "HV")
                    RegistrarMudancaPixel(w - 1 - px, py, cor);
                if (mirrorMode == "V" || mirrorMode == "HV")
                    RegistrarMudancaPixel(px, h - 1 - py, cor);
                if (mirrorMode == "HV")
                    RegistrarMudancaPixel(w - 1 - px, h - 1 - py, cor);
            }
        }
        private void PickColorFromCanvas(int x, int y, MouseButtons botao)
        {
            Color picked = canvasBitmap.GetPixel(x, y);
            if (botao == MouseButtons.Left)
                currentColor = picked;
            else if (botao == MouseButtons.Right)
                secondaryColor = picked;
            AtualizarIndicadoresDeCor();
        }
        private void ReplaceAllPixelsOfColor(Color targetColor, Color replacementColor)
        {
            if (targetColor.ToArgb() == replacementColor.ToArgb())
                return;

            IniciarAcao();

            for (int i = 0; i < canvasBitmap.Width; i++)
            {
                for (int j = 0; j < canvasBitmap.Height; j++)
                {
                    if (canvasBitmap.GetPixel(i, j).ToArgb() == targetColor.ToArgb())
                    {
                        RegistrarMudancaPixel(i, j, replacementColor);
                    }
                }
            }

            FinalizarAcao();
            pictureBoxCanvas.Invalidate();
        }
        private void PictureBoxCanvas_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < canvasBitmap.Width; i++)
            {
                for (int j = 0; j < canvasBitmap.Height; j++)
                {
                    using (Brush b = new SolidBrush(canvasBitmap.GetPixel(i, j)))
                        e.Graphics.FillRectangle(b, i * pixelSize, j * pixelSize, pixelSize, pixelSize);
                }
            }

            // --- Preview de formas (pixel-perfect) ---
            if (isDrawing && startPoint != null)
            {
                Color corPreview = mouseButtonInUse == MouseButtons.Left ? currentColor : secondaryColor;

                if (currentTool == "Rectangle" || currentTool == "Circle")
                {
                    Bitmap preview = GerarPreviewForma();
                    if (preview != null)
                    {
                        for (int i = 0; i < preview.Width; i++)
                        {
                            for (int j = 0; j < preview.Height; j++)
                            {
                                Color pixel = preview.GetPixel(i, j);
                                if (pixel.A == 0) continue;
                                e.Graphics.FillRectangle(new SolidBrush(pixel), i * pixelSize, j * pixelSize, pixelSize, pixelSize);
                            }
                        }
                    }
                }
                else if (currentTool == "Line")
                {
                    Color corUsar = mouseButtonInUse == MouseButtons.Left ? currentColor : secondaryColor;
                    List<Point> pixels = ObterPixelsLinhaBresenham(startPoint.Value.X, startPoint.Value.Y, currentPoint.X, currentPoint.Y);

                    // Draw main line and all mirrors
                    foreach (var p in GetMirroredPoints(pixels, canvasBitmap.Width, canvasBitmap.Height, mirrorMode))
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(128, corUsar)))
                            e.Graphics.FillRectangle(b, p.X * pixelSize, p.Y * pixelSize, pixelSize, pixelSize);
                    }
                }
            }
            // Pencil/Eraser preview
            else if (isDrawing && (currentTool == "Pencil" || currentTool == "Eraser") && lastPreviewPoint != null)
            {
                Color corPreview = currentTool == "Eraser" ? Color.Transparent : (mouseButtonInUse == MouseButtons.Left ? currentColor : secondaryColor);
                var previewPixels = ObterPixelsLinhaBresenham(lastPreviewPoint.Value.X, lastPreviewPoint.Value.Y, currentPoint.X, currentPoint.Y);

                foreach (var p in GetMirroredPoints(previewPixels, canvasBitmap.Width, canvasBitmap.Height, mirrorMode))
                {
                    Color drawColor = corPreview == Color.Transparent
                        ? Color.FromArgb(80, 200, 200, 200)
                        : Color.FromArgb(120, corPreview.R, corPreview.G, corPreview.B);

                    using (Brush b = new SolidBrush(drawColor))
                        e.Graphics.FillRectangle(b, p.X * pixelSize, p.Y * pixelSize, pixelSize, pixelSize);
                }
            }

            // --- Grid ---
            Pen gridPen = new Pen(Color.FromArgb(40, 0, 0, 0)); // Grid semi-transparente
            for (int i = 0; i <= canvasBitmap.Width; i++)
                e.Graphics.DrawLine(gridPen, i * pixelSize, 0, i * pixelSize, canvasBitmap.Height * pixelSize);
            for (int j = 0; j <= canvasBitmap.Height; j++)
                e.Graphics.DrawLine(gridPen, 0, j * pixelSize, canvasBitmap.Width * pixelSize, j * pixelSize);
        }

        // Helper for mirror preview
        private IEnumerable<Point> GetMirroredPoints(IEnumerable<Point> basePoints, int w, int h, string mirrorMode)
        {
            HashSet<Point> result = new HashSet<Point>();
            foreach (var p in basePoints)
            {
                if (p.X >= 0 && p.Y >= 0 && p.X < w && p.Y < h)
                    result.Add(p);

                if (mirrorMode == "H" || mirrorMode == "HV")
                {
                    var mp = new Point(w - 1 - p.X, p.Y);
                    if (mp.X >= 0 && mp.Y >= 0 && mp.X < w && mp.Y < h)
                        result.Add(mp);
                }
                if (mirrorMode == "V" || mirrorMode == "HV")
                {
                    var mp = new Point(p.X, h - 1 - p.Y);
                    if (mp.X >= 0 && mp.Y >= 0 && mp.X < w && mp.Y < h)
                        result.Add(mp);
                }
                if (mirrorMode == "HV")
                {
                    var mp = new Point(w - 1 - p.X, h - 1 - p.Y);
                    if (mp.X >= 0 && mp.Y >= 0 && mp.X < w && mp.Y < h)
                        result.Add(mp);
                }
            }
            return result;
        }

        private List<Point> ObterPixelsLinhaBresenham(int x0, int y0, int x1, int y1)
        {
            List<Point> pixels = new List<Point>();
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                pixels.Add(new Point(x0, y0));
                if (x0 == x1 && y0 == y1)
                    break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return pixels;
        }

        // Substitua apenas este método no seu código atual:
        private void FloodFill(int x, int y, Color targetColor, Color replacementColor)
        {
            if (targetColor.ToArgb() == replacementColor.ToArgb())
                return;

            Rectangle rect = new Rectangle(0, 0, canvasBitmap.Width, canvasBitmap.Height);
            BitmapData data = canvasBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int bytes = Math.Abs(data.Stride) * data.Height;
            byte[] buffer = new byte[bytes];
            Marshal.Copy(data.Scan0, buffer, 0, bytes);

            int GetIndex(int px, int py) => (py * data.Stride) + (px * 4);
            int target = targetColor.ToArgb();

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(x, y));

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                if (p.X < 0 || p.Y < 0 || p.X >= canvasBitmap.Width || p.Y >= canvasBitmap.Height)
                    continue;

                int index = GetIndex(p.X, p.Y);
                int color =
                    buffer[index] |
                    (buffer[index + 1] << 8) |
                    (buffer[index + 2] << 16) |
                    (buffer[index + 3] << 24);

                if (color != target)
                    continue;

                // Define a nova cor
                buffer[index] = replacementColor.B;
                buffer[index + 1] = replacementColor.G;
                buffer[index + 2] = replacementColor.R;
                buffer[index + 3] = replacementColor.A;

                // Enfileira os 4 vizinhos
                queue.Enqueue(new Point(p.X + 1, p.Y));
                queue.Enqueue(new Point(p.X - 1, p.Y));
                queue.Enqueue(new Point(p.X, p.Y + 1));
                queue.Enqueue(new Point(p.X, p.Y - 1));
            }

            Marshal.Copy(buffer, 0, data.Scan0, bytes);
            canvasBitmap.UnlockBits(data);
            pictureBoxCanvas.Invalidate();
        }

        private void ConfigurarAtalhos()
        {
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.S)
                    ExportarPNG();

                else if (e.Control && e.KeyCode == Keys.Z)
                    Undo();

                else if (e.Control && e.KeyCode == Keys.B)
                    toolButtons["ReplaceColor"].PerformClick();

                else if (e.Shift && e.KeyCode == Keys.H)
                    btnMirrorH.PerformClick();

                else if (e.Shift && e.KeyCode == Keys.V)
                    btnMirrorV.PerformClick();

                else if (e.KeyCode == Keys.P)
                    toolButtons["Pencil"].PerformClick();

                else if (e.KeyCode == Keys.E)
                    toolButtons["Eraser"].PerformClick();

                else if (e.KeyCode == Keys.B)
                    toolButtons["Bucket"].PerformClick();

                else if (e.KeyCode == Keys.C)
                    toolButtons["Circle"].PerformClick();

                else if (e.KeyCode == Keys.R)
                    toolButtons["Rectangle"].PerformClick();

                else if (e.KeyCode == Keys.L)
                    toolButtons["Line"].PerformClick();

                else if (e.KeyCode == Keys.Q)
                    toolButtons["ColorPicker"].PerformClick();
            };
        }


        private void DesenharForma(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p2.X - p1.X);
            int h = Math.Abs(p2.Y - p1.Y);

            using (Graphics g = Graphics.FromImage(canvasBitmap))
            {
                Pen pen = new Pen(currentColor, 1);
                if (currentTool == "Rectangle")
                    g.DrawRectangle(pen, x, y, w, h);
                else if (currentTool == "Circle")
                    g.DrawEllipse(pen, x, y, w, h);
            }
        }

        private void LimparCanvas()
        {
            using (Graphics g = Graphics.FromImage(canvasBitmap))
                g.Clear(Color.Transparent);
            pictureBoxCanvas.Invalidate();
        }

        // Estrutura para armazenar cada alteração
        public class PixelAction
        {
            public List<(int X, int Y, Color OldColor, Color NewColor)> Changes = new();
        }

        // Pilhas de desfazer/refazer
        private Stack<PixelAction> undoStack = new();
        private Stack<PixelAction> redoStack = new();

        // Ao desenhar um pixel,registre as mudanças:
        private PixelAction currentAction = null;

        private void IniciarAcao()
        {
            currentAction = new PixelAction();
            pixelsChangedThisAction.Clear();
        }

        private HashSet<(int, int)> pixelsChangedThisAction = new();

        private void RegistrarMudancaPixel(int x, int y, Color newColor)
        {
            if (currentAction == null) return;
            if (!pixelsChangedThisAction.Add((x, y))) return; // Only register the first change per pixel in this action

            Color oldColor = canvasBitmap.GetPixel(x, y);
            if (oldColor != newColor)
            {
                currentAction.Changes.Add((x, y, oldColor, newColor));
                canvasBitmap.SetPixel(x, y, newColor);
            }
        }

        private void FinalizarAcao()
        {
            if (currentAction != null && currentAction.Changes.Count > 0)
            {
                undoStack.Push(currentAction);
                redoStack.Clear();
            }
            currentAction = null;
            pixelsChangedThisAction.Clear();
            pictureBoxCanvas.Invalidate();
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;

            PixelAction lastAction = undoStack.Pop();
            redoStack.Push(lastAction);

            foreach (var (x, y, oldColor, _) in lastAction.Changes)
                canvasBitmap.SetPixel(x, y, oldColor);

            pictureBoxCanvas.Invalidate();
        }

        private void Redo()
        {
            if (redoStack.Count == 0) return;

            PixelAction nextAction = redoStack.Pop();
            undoStack.Push(nextAction);

            foreach (var (x, y, _, newColor) in nextAction.Changes)
                canvasBitmap.SetPixel(x, y, newColor);

            pictureBoxCanvas.Invalidate();
        }


        private List<Point> ObterPixelsForma(Point p1, Point p2, string ferramenta)
        {
            List<Point> pixels = new List<Point>();
            int w = canvasBitmap.Width;
            int h = canvasBitmap.Height;

            switch (ferramenta)
            {
                case "Rectangle":
                    {
                        int x = Math.Min(p1.X, p2.X);
                        int y = Math.Min(p1.Y, p2.Y);
                        int width = Math.Abs(p2.X - p1.X);
                        int height = Math.Abs(p2.Y - p1.Y);

                        for (int i = x; i <= x + width; i++)
                        {
                            pixels.Add(new Point(i, y));
                            pixels.Add(new Point(i, y + height));
                        }
                        for (int j = y + 1; j < y + height; j++)
                        {
                            pixels.Add(new Point(x, j));
                            pixels.Add(new Point(x + width, j));
                        }
                    }
                    break;

                case "Circle":
                    {
                        int x0 = Math.Min(p1.X, p2.X);
                        int y0 = Math.Min(p1.Y, p2.Y);
                        int x1 = Math.Max(p1.X, p2.X);
                        int y1 = Math.Max(p1.Y, p2.Y);

                        int a = (x1 - x0) / 2;
                        int b = (y1 - y0) / 2;
                        int xc = x0 + a;
                        int yc = y0 + b;

                        if (a == 0 || b == 0)
                            break;

                        int a2 = a * a, b2 = b * b;
                        int fa2 = 4 * a2, fb2 = 4 * b2;
                        int x, y, sigma;

                        // First half
                        for (x = 0, y = b, sigma = 2 * b2 + a2 * (1 - 2 * b); b2 * x <= a2 * y; x++)
                        {
                            pixels.Add(new Point(xc + x, yc + y));
                            pixels.Add(new Point(xc - x, yc + y));
                            pixels.Add(new Point(xc + x, yc - y));
                            pixels.Add(new Point(xc - x, yc - y));
                            if (sigma >= 0)
                            {
                                sigma += fa2 * (1 - y);
                                y--;
                            }
                            sigma += b2 * ((4 * x) + 6);
                        }
                        // Second half
                        for (x = a, y = 0, sigma = 2 * a2 + b2 * (1 - 2 * a); a2 * y <= b2 * x; y++)
                        {
                            pixels.Add(new Point(xc + x, yc + y));
                            pixels.Add(new Point(xc - x, yc + y));
                            pixels.Add(new Point(xc + x, yc - y));
                            pixels.Add(new Point(xc - x, yc - y));
                            if (sigma >= 0)
                            {
                                sigma += fb2 * (1 - x);
                                x--;
                            }
                            sigma += a2 * ((4 * y) + 6);
                        }
                    }
                    break;

                case "Line":
                    pixels = ObterPixelsLinhaBresenham(p1.X, p1.Y, p2.X, p2.Y);
                    break;
            }

            // Aplica espelho nos pixels
            HashSet<Point> finalPixels = new HashSet<Point>(pixels);
            foreach (var p in pixels)
            {
                if (mirrorMode == "H" || mirrorMode == "HV")
                    finalPixels.Add(new Point(w - 1 - p.X, p.Y));
                if (mirrorMode == "V" || mirrorMode == "HV")
                    finalPixels.Add(new Point(p.X, h - 1 - p.Y));
                if (mirrorMode == "HV")
                    finalPixels.Add(new Point(w - 1 - p.X, h - 1 - p.Y));
            }

            // Remove pixels out of bounds
            return finalPixels.Where(p => p.X >= 0 && p.Y >= 0 && p.X < w && p.Y < h).ToList();
        }

        private void DesenharFormaFinal(Point p1, Point p2)
        {
            Color corUsar = mouseButtonInUse == MouseButtons.Left ? currentColor : secondaryColor;
            List<Point> pixels = ObterPixelsForma(p1, p2, currentTool);

            foreach (var p in pixels)
                RegistrarMudancaPixel(p.X, p.Y, corUsar); // Register each pixel change

            pictureBoxCanvas.Invalidate();
        }

        private Bitmap GerarPreviewForma()
        {
            if (startPoint == null) return null;

            Bitmap preview = (Bitmap)canvasBitmap.Clone();
            Color corUsar = mouseButtonInUse == MouseButtons.Left ? currentColor : secondaryColor;
            Color corPreview = Color.FromArgb(120, corUsar.R, corUsar.G, corUsar.B); // semi-transparente

            List<Point> pixels = ObterPixelsForma(startPoint.Value, currentPoint, currentTool);

            foreach (var p in pixels)
                preview.SetPixel(p.X, p.Y, corPreview);

            return preview;
        }

        private void DesenharLinhaBresenham(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && y0 >= 0 && x0 < canvasBitmap.Width && y0 < canvasBitmap.Height)
                    canvasBitmap.SetPixel(x0, y0, color);

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private Point? lastPreviewPoint = null;
        private bool removeColorMode = false;

        private void AtivarEspelho(string mode)
        {
            // Toggle mirror mode between None and the selected mode
            mirrorMode = mirrorMode == mode ? "None" : mode; 
            AtualizarBotoesEspelho();
        }

        private void AtualizarBotoesEspelho()
        {
            btnMirrorH.BackColor = mirrorMode == "H" || mirrorMode == "HV" ? Color.DarkCyan : FUNDOBTN; // Highlight if active
            btnMirrorV.BackColor = mirrorMode == "V" || mirrorMode == "HV" ? Color.DarkCyan : FUNDOBTN;
            btnMirrorHV.BackColor = mirrorMode == "HV" ? Color.DarkCyan : FUNDOBTN;
        }

        private void AtualizarIndicadoresDeCor()
        {
            if (panelLeftColor != null) panelLeftColor.BackColor = currentColor;
            if (panelRightColor != null) panelRightColor.BackColor = secondaryColor;
        }

        private void StartOver()
        {
            // Confirm with the user
            var result = MessageBox.Show("Are you sure you want to start over? This will erase all frames.",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            // Clear all frames and create a fresh empty frame
            IniciarAcao(); // start undo action

            frames.Clear();
            Bitmap newFrame = new Bitmap(canvasBitmap.Width, canvasBitmap.Height);
            using (Graphics g = Graphics.FromImage(newFrame))
                g.Clear(Color.Transparent);

            frames.Add(newFrame);
            currentFrameIndex = 0;
            canvasBitmap = newFrame;

            // Register all pixels as changed for undo
            for (int x = 0; x < canvasBitmap.Width; x++)
            {
                for (int y = 0; y < canvasBitmap.Height; y++)
                {
                    RegistrarMudancaPixel(x, y, Color.Transparent);
                }
            }

            FinalizarAcao(); // push to undo stack
            pictureBoxCanvas.Invalidate();
        }

        private List<Bitmap> frames = new(); // Lista de frames para animação
        private int currentFrameIndex = 0; // Índice do frame atual
    }

}
