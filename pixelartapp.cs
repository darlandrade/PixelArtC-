using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PixelArt
{
    public partial class Form1 : Form
    {
        private Panel panelFerramentas;
        private Panel panelCanvas;
        private Panel panelFuncoes;
        private Panel panelRodape;
        private PictureBox pictureBoxCanvas;

        private Bitmap canvasBitmap;
        private Color currentColor = Color.Black;
        private int pixelSize = 20;
        private bool isDrawing = false;
        private string currentTool = "Pencil"; // Lápis, Borracha, Balde
        // --- Variáveis para formas ---
        private Point? startPoint = null;   // ponto inicial do clique
        private Point currentPoint;         // posição atual do mouse

        private MouseButtons mouseButtonInUse; // Botão que iniciou o desenho
        private Color secondaryColor = Color.Chartreuse; // Cor do botão direito

        // Espelho
        private string mirrorMode = "None"; // "None", "H", "V", "HV"
        private Button btnMirrorH;
        private Button btnMirrorV;
        private Button btnMirrorHV;

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
                BackColor = Color.LightGray
            };
            Controls.Add(panelFerramentas);

            // Painel direito - Funções (paleta)
            panelFuncoes = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = Color.LightGray,
                Padding = new Padding(10),
                AutoScroll = true
            };
            Controls.Add(panelFuncoes);

            // Painel inferior - Rodapé
            panelRodape = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.LightGray,
                Padding = new Padding(10)
            };
            Controls.Add(panelRodape);

            // Painel central - Canvas
            panelCanvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Gray, // dá contraste para ver o canvas
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
            int top = 20;

            Button btnPencil = new Button { Text = "Lápis", Left = 10, Top = top, Width = 100 };
            btnPencil.Click += (s, e) => currentTool = "Pencil";
            panelFerramentas.Controls.Add(btnPencil);

            Button btnEraser = new Button { Text = "Borracha", Left = 10, Top = top + 40, Width = 100 };
            btnEraser.Click += (s, e) => currentTool = "Eraser";
            panelFerramentas.Controls.Add(btnEraser);

            Button btnBucket = new Button { Text = "Balde", Left = 10, Top = top + 80, Width = 100 };
            btnBucket.Click += (s, e) => currentTool = "Bucket";
            panelFerramentas.Controls.Add(btnBucket);

            Button btnRectangle = new Button { Text = "Retângulo", Left = 10, Top = top + 120, Width = 100 };
            btnRectangle.Click += (s, e) => currentTool = "Rectangle";
            panelFerramentas.Controls.Add(btnRectangle);

            Button btnCircle = new Button { Text = "Círculo", Left = 10, Top = top + 160, Width = 100 };
            btnCircle.Click += (s, e) => currentTool = "Circle";
            panelFerramentas.Controls.Add(btnCircle);

            Button btnLine = new Button { Text = "Linha", Left = 10, Top = top + 200, Width = 100 };
            btnLine.Click += (s, e) => currentTool = "Line";
            panelFerramentas.Controls.Add(btnLine);

        }

        private Panel panelLeftColor;
        private Panel panelRightColor;

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
                    FlatStyle = FlatStyle.Flat
                };

                colorBtn.MouseDown += (s, e) =>
                {
                    if (!colorBtn.Enabled) return;
                    if (e.Button == MouseButtons.Left) currentColor = colorBtn.BackColor;
                    else if (e.Button == MouseButtons.Right) secondaryColor = colorBtn.BackColor;
                    AtualizarIndicadoresDeCor();
                };

                panelFuncoes.Controls.Add(colorBtn);
            }

            // Botão "+"
            Button btnAddColor = new Button
            {
                Text = "+",
                Width = 30,
                Height = 30,
                Left = 10,
                Top = inicioY + 3 * (btnSize + margem) + 10
            };
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
            panelFuncoes.Controls.Add(btnAddColor);

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
                BorderStyle = BorderStyle.FixedSingle
            };
            panelFuncoes.Controls.Add(panelRightColor);

            int mirrorTop = btnAddColor.Bottom + 10;

            btnMirrorH = new Button
            {
                Text = "H",
                Width = 30,
                Height = 30,
                Left = 10,
                Top = mirrorTop,
                BackColor = Color.LightGray
            };
            btnMirrorH.Click += (s, e) => AtivarEspelho("H");
            panelFuncoes.Controls.Add(btnMirrorH);

            btnMirrorV = new Button
            {
                Text = "V",
                Width = 30,
                Height = 30,
                Left = btnMirrorH.Right + 5,
                Top = mirrorTop,
                BackColor = Color.LightGray
            };
            btnMirrorV.Click += (s, e) => AtivarEspelho("V");
            panelFuncoes.Controls.Add(btnMirrorV);

            btnMirrorHV = new Button
            {
                Text = "H+V",
                Width = 45,
                Height = 30,
                Left = btnMirrorV.Right + 5,
                Top = mirrorTop,
                BackColor = Color.LightGray
            };
            btnMirrorHV.Click += (s, e) => AtivarEspelho("HV");
            panelFuncoes.Controls.Add(btnMirrorHV);

        }

        // Atualiza os painéis de cor
        private void AtualizarIndicadoresDeCor()
        {
            if (panelLeftColor != null) panelLeftColor.BackColor = currentColor;
            if (panelRightColor != null) panelRightColor.BackColor = secondaryColor;
        }

        private void AtualizarPaleta()
        {
            int i = 0;
            foreach (Control c in panelFuncoes.Controls)
            {
                if (c is Button btn && btn.Text == "")
                {
                    if (i < paletteColors.Count)
                    {
                        btn.BackColor = paletteColors[i];
                        btn.Enabled = true;
                    }
                    else
                    {
                        btn.BackColor = Color.LightGray; // indica que não há cor
                        btn.Enabled = false;
                    }
                    i++;
                }
            }
        }

        private void AtivarEspelho(string mode)
        {
            mirrorMode = mirrorMode == mode ? "None" : mode; // alterna ao clicar novamente
            AtualizarBotoesEspelho();
        }

        private void AtualizarBotoesEspelho()
        {
            btnMirrorH.BackColor = mirrorMode == "H" || mirrorMode == "HV" ? Color.LightBlue : Color.LightGray;
            btnMirrorV.BackColor = mirrorMode == "V" || mirrorMode == "HV" ? Color.LightBlue : Color.LightGray;
            btnMirrorHV.BackColor = mirrorMode == "HV" ? Color.LightBlue : Color.LightGray;
        }

        private void CriarRodape()
        {
            Button btnExportar = new Button
            {
                Text = "Exportar PNG",
                Width = 120,
                Height = 30,
                Left = 10,
                Top = 10
            };
            btnExportar.Click += (s, e) => ExportarPNG();
            panelRodape.Controls.Add(btnExportar);
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

    IniciarAcao(); // Start a new undo action

    if (currentTool == "Rectangle" || currentTool == "Circle" || currentTool == "Line")
    {
        startPoint = new Point(x, y);
        currentPoint = startPoint.Value;
    }
    else
    {
        AplicarFerramenta(e.X, e.Y, e.Button);
    }
}

private void PictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
{
    int x = e.X / pixelSize;
    int y = e.Y / pixelSize;

    if (!isDrawing) return;

    if (x < 0 || y < 0 || x >= canvasBitmap.Width || y >= canvasBitmap.Height)
        return;

    currentPoint = new Point(x, y);

    if (currentTool == "Rectangle" || currentTool == "Circle" || currentTool == "Line")
    {
        pictureBoxCanvas.Invalidate(); // Show preview
    }
    else
    {
        AplicarFerramenta(e.X, e.Y, mouseButtonInUse);
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
    FinalizarAcao(); // End the undo action
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
                    foreach (var p in pixels)
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(128, corUsar))) // 128 = 50% de transparência
                            e.Graphics.FillRectangle(b, p.X * pixelSize, p.Y * pixelSize, pixelSize, pixelSize);
                    }
                }

            }

            // --- Grid ---
            Pen gridPen = new Pen(Color.FromArgb(40, 0, 0, 0)); // Grid semi-transparente
            for (int i = 0; i <= canvasBitmap.Width; i++)
                e.Graphics.DrawLine(gridPen, i * pixelSize, 0, i * pixelSize, canvasBitmap.Height * pixelSize);
            for (int j = 0; j <= canvasBitmap.Height; j++)
                e.Graphics.DrawLine(gridPen, 0, j * pixelSize, canvasBitmap.Width * pixelSize, j * pixelSize);
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

        // Ao desenhar um pixel, registre as mudanças:
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
                        int x = Math.Min(p1.X, p2.X);
                        int y = Math.Min(p1.Y, p2.Y);
                        int width = Math.Abs(p2.X - p1.X);
                        int height = Math.Abs(p2.Y - p1.Y);

                        double rx = width / 2.0;
                        double ry = height / 2.0;
                        double cx = x + rx;
                        double cy = y + ry;

                        for (int i = x; i <= x + width; i++)
                        {
                            for (int j = y; j <= y + height; j++)
                            {
                                double dx = i - cx;
                                double dy = j - cy;
                                double dist = Math.Pow(dx / rx, 2) + Math.Pow(dy / ry, 2);
                                if (dist >= 0.85 && dist <= 1.15) // contorno mais preciso
                                    pixels.Add(new Point(i, j));
                            }
                        }
                    }
                    break;

                case "Line":
                    pixels = ObterPixelsLinhaBresenham(p1.X, p1.Y, p2.X, p2.Y);
                    break;
            }

            // Aplica espelho nos pixels
            List<Point> todosPixels = new List<Point>(pixels);
            foreach (var p in pixels)
            {
                if (mirrorMode == "H" || mirrorMode == "HV")
                    todosPixels.Add(new Point(w - 1 - p.X, p.Y));
                if (mirrorMode == "V" || mirrorMode == "HV")
                    todosPixels.Add(new Point(p.X, h - 1 - p.Y));
                if (mirrorMode == "HV")
                    todosPixels.Add(new Point(w - 1 - p.X, h - 1 - p.Y));
            }

            // Remove duplicados fora do canvas
            List<Point> finalPixels = new List<Point>();
            foreach (var p in todosPixels)
            {
                if (p.X >= 0 && p.Y >= 0 && p.X < w && p.Y < h && !finalPixels.Contains(p))
                    finalPixels.Add(p);
            }

            return finalPixels;
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
    }

}
