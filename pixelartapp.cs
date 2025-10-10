using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualBasic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using YahooFinanceApi;

namespace Monitor_Financeiro_2
{
    public partial class Form1 : Form
    {
        private DataGridView grid;
        private Button btnAdicionar, btnAtualizar, btnRemover;
        private List<AcaoInfo> acoes = new List<AcaoInfo>();
        private readonly string arquivoJson = "acoes.json";
        private Label lblTotalInvestido;
        private Label lblGanhoPerda;

        private decimal fundosDisponiveis = 0; // saldo inicial
        private Label lblFundos;

        readonly Color FUNDOJANELA = Color.FromArgb(28, 28, 27);
        readonly Color FUNDOBTN = Color.FromArgb(30, 30, 30);
        readonly Color BTNBORDER = Color.FromArgb(77, 77, 75);
        readonly Color MOUSEHOVERBTN = Color.FromArgb(54, 14, 17);



        public Form1()
        {
            InitializeComponent();
            InicializarInterface();
            AplicarModoEscuro();
            CarregarDados();
            CarregarFundos();
            fundosDisponiveis = CarregarFundos();
            AtualizarFundosLabel();

            // Define o ícone
            //this.Icon = new Icon("icone.ico");

            void AplicarModoEscuro()
            {
                // Fundo geral do app
                this.BackColor = Color.FromArgb(30, 30, 30);
                this.ForeColor = Color.White;

                // Ajusta a cor de todos os controles
                foreach (Control ctrl in this.Controls)
                {
                    AplicarEstiloEscuro(ctrl);
                }

                // Estilo do DataGridView
                grid.BackgroundColor = Color.FromArgb(25, 25, 25);
                grid.GridColor = Color.FromArgb(60, 60, 60);

                // Desativa o visual padrão do Windows (resolve o azul no cabeçalho)
                grid.EnableHeadersVisualStyles = false;

                // Cores principais do corpo do grid
                grid.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);  // cinza escuro para seleção
                grid.DefaultCellStyle.ForeColor = Color.Gray;             // cinza claro (melhor contraste)
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70); // fundo levemente mais claro
                grid.DefaultCellStyle.SelectionForeColor = Color.White; // branco para seleção

                // Garante que linhas alternadas fiquem visíveis
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 40); // fundo um pouco diferente
                grid.AlternatingRowsDefaultCellStyle.ForeColor = Color.Gray; // cinza claro

                // Bordas e grades mais suaves
                grid.GridColor = Color.FromArgb(60, 60, 60);


                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50); // cor de fundo do cabeçalho
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; // cor da fonte do cabeçalho
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 55, 58); // mesma cor para não mudar
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; // centraliza o texto dos cabeçalhos
                grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold); // negrito no cabeçalho

                grid.EnableHeadersVisualStyles = false;
            }


        }        
        private void InicializarInterface()
        {
            this.Text = "Monitor Financeiro";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1225, 700);

            // Painel esquerdo com botões
            FlowLayoutPanel panelBotoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 200,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true
            };

            btnAdicionar = new Button { Text = "Adicionar Ticker", Width = 160, Height = 35, Margin = new Padding(0, 0, 0, 10) };
            btnAtualizar = new Button { Text = "Atualizar Ação", Width = 160, Height = 35, Margin = new Padding(0, 0, 0, 10) };
            btnRemover = new Button { Text = "Remover", Width = 160, Height = 35, Margin = new Padding(0, 0, 0, 10) };
            Button btnAdicionarFundos = new Button { Text = "Adicionar Saldo", Width = 160, Height = 35, Margin = new Padding(0, 0, 0, 10) };
            Button btnAbrirGoogleFinance = new Button { Text = "Google Finance", Width = 160, Height = 35, Margin = new Padding(0, 0, 0, 10) };

            btnAdicionar.Click += BtnAdicionar_Click;
            btnAtualizar.Click += async (s, e) => await AtualizarValoresAsync();
            btnRemover.Click += BtnRemover_Click;
            btnAdicionarFundos.Click += BtnAdicionarFundos_Click;

            AplicarMouseHoverBotoes(btnAdicionar);
            AplicarMouseHoverBotoes(btnAtualizar);
            AplicarMouseHoverBotoes(btnRemover);
            AplicarMouseHoverBotoes(btnAdicionarFundos);
            AplicarMouseHoverBotoes(btnAbrirGoogleFinance);

            panelBotoes.Controls.Add(btnAdicionar);
            panelBotoes.Controls.Add(btnAtualizar);
            panelBotoes.Controls.Add(btnRemover);
            panelBotoes.Controls.Add(btnAdicionarFundos);
            panelBotoes.Controls.Add(btnAbrirGoogleFinance);

            // Evento de clique → abre o navegador padrão
            btnAbrirGoogleFinance.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://www.google.com/finance/?hl=pt",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao abrir o link: {ex.Message}");
                }
            };

            

            // Painel direito com tabela e labels
            Panel panelDireito = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Labels de resumo no topo
            FlowLayoutPanel panelResumo = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 0, 0, 10)
            };

            lblTotalInvestido = new Label
            {
                Text = "Investido: R$ 0,00",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 20, 0)
            };

            lblGanhoPerda = new Label
            {
                Text = "Ganho / Perda: R$ 0,00",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 20, 0)
            };

            lblFundos = new Label
            {
                Text = $"Saldo: R$ {fundosDisponiveis:F2}",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 20, 0)
            };
            panelResumo.Controls.Add(lblFundos);
            panelResumo.Controls.Add(lblTotalInvestido);
            panelResumo.Controls.Add(lblGanhoPerda);

            // DataGridView
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            grid.Columns.Add("acao", "Ação");
            grid.Columns.Add("qtde", "Qtde total (simulada)");
            grid.Columns.Add("precoMedio", "Preço médio (R$)");
            grid.Columns.Add("precoAtual", "Preço atual (R$)");
            grid.Columns.Add("valorAtual", "Total Ivto. (R$)");
            grid.Columns.Add("stop", "Stop Loss (R$)");
            grid.Columns.Add("alerta", "Alerta");
            grid.Columns.Add("alvo", "Alvo (R$)");
            grid.Columns.Add("valorizacao", "Valorização (%)");
            grid.Columns.Add("data", "Data");

            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AllowUserToResizeColumns = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            // Centraliza o texto dos cabeçalhos
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            // (opcional) deixa o cabeçalho em negrito para destacar
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            // Ajuste automático por coluna
            grid.Columns["acao"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["qtde"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["precoMedio"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["precoAtual"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["valorAtual"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["stop"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["alerta"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["alvo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            // A coluna de valorização é importante, mas não tão larga:
            grid.Columns["valorizacao"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            // A coluna de data pode ter largura fixa, pois o conteúdo é previsível
            grid.Columns["data"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            grid.Columns["data"].Width = 60;

            panelDireito.Controls.Add(grid);
            panelDireito.Controls.Add(panelResumo);

            // Adiciona os dois painéis ao formulário
            this.Controls.Add(panelDireito);
            this.Controls.Add(panelBotoes);
        }

        // Aplica mouse hover a todos os botões
        void AplicarMouseHoverBotoes(Button btn)
        {
            btn.BackColor = FUNDOBTN;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = BTNBORDER;
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { btn.BackColor = MOUSEHOVERBTN; };
            btn.MouseLeave += (s, e) => { btn.BackColor = FUNDOBTN; };
        }

        private void AplicarEstiloEscuro(Control ctrl)
        {
            if (ctrl is Panel || ctrl is GroupBox)
            {
                ctrl.BackColor = FUNDOJANELA;
                ctrl.ForeColor = Color.White;
            }
            else if (ctrl is Button btn)
            {
                btn.BackColor = FUNDOBTN;
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = BTNBORDER;
            }
            else if (ctrl is Label lbl)
            {
                lbl.ForeColor = Color.White;
            }
            else if (ctrl is TextBox txt)
            {
                txt.BackColor = Color.FromArgb(45, 45, 45);
                txt.ForeColor = Color.White;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }

            // aplica recursivamente aos filhos
            foreach (Control child in ctrl.Controls)
                AplicarEstiloEscuro(child);
        }

        private void CarregarDados()
        {
            if (File.Exists(arquivoJson))
            {
                try
                {
                    string json = File.ReadAllText(arquivoJson);
                    acoes = JsonSerializer.Deserialize<List<AcaoInfo>>(json) ?? new List<AcaoInfo>();
                    AtualizarTabela();
                }
                catch { /* Ignorar erros de leitura */ }
            }
        }

        private void SalvarDados()
        {
            try
            {
                string json = JsonSerializer.Serialize(acoes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(arquivoJson, json);
            }
            catch { /* Ignorar erros de escrita */ }
        }

        private void BtnRemover_Click(object sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione uma ação para remover.");
                return;
            }

            var row = grid.SelectedRows[0];
            string ticker = row.Cells["acao"].Value.ToString();

            if (MessageBox.Show($"Remover {ticker}?", "Confirmar remoção", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                acoes.RemoveAll(a => a.Ticker == ticker);
                AtualizarTabela();
                SalvarDados();
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string ticker = grid.Rows[e.RowIndex].Cells["acao"].Value.ToString();
            var acao = acoes.FirstOrDefault(a => a.Ticker == ticker);
            if (acao != null)
                AbrirFormularioAcao(acao);
        }

        private void BtnAdicionar_Click(object sender, EventArgs e)
        {
            AbrirFormularioAcao(null);
        }


        private void AbrirFormularioAcao(AcaoInfo acaoExistente)
        {
            using (Form form = new Form())
            {
                form.Text = acaoExistente == null ? "Adicionar Ação" : $"Editar {acaoExistente.Ticker}";
                form.StartPosition = FormStartPosition.CenterParent;
                form.Width = 360;
                form.Height = 420;

                Label lblTicker = new Label { Text = "Ticker:", Left = 20, Top = 20, Width = 120 };
                ComboBox cmbTicker = new ComboBox
                {
                    Left = 150,
                    Top = 20,
                    Width = 150,
                    DropDownStyle = ComboBoxStyle.DropDown,
                    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                    AutoCompleteSource = AutoCompleteSource.ListItems
                };

                foreach (var a in acoes)
                    cmbTicker.Items.Add(a.Ticker);

                Label lblPreco = new Label { Text = "Valor da Ação (R$):", Left = 20, Top = 60, Width = 120 };
                TextBox txtPreco = new TextBox { Left = 150, Top = 60, Width = 150 };

                Label lblQtde = new Label { Text = "Quantidade:", Left = 20, Top = 100, Width = 120 };
                TextBox txtQtde = new TextBox { Left = 150, Top = 100, Width = 150 };

                Label lblStop = new Label { Text = "Stop Loss (R$):", Left = 20, Top = 140, Width = 120 };
                TextBox txtStop = new TextBox { Left = 150, Top = 140, Width = 150 };

                CheckBox chkStopAuto = new CheckBox
                {
                    Text = "Stop Automático (5%)",
                    Left = 150,
                    Top = 170,
                    AutoSize = true
                };

                Label lblAlvo = new Label { Text = "Resistência / Alvo (R$):", Left = 20, Top = 200, Width = 120 };
                TextBox txtAlvo = new TextBox { Left = 150, Top = 200, Width = 150 };

                Button btnSalvar = new Button { Text = "Salvar", Left = 110, Top = 250, Width = 100 };

                // Configuração inicial do formulário
                if (acaoExistente != null)
                {
                    // Edição: mantém stop manual
                    cmbTicker.Text = acaoExistente.Ticker;
                    cmbTicker.Enabled = false;
                    txtPreco.Text = acaoExistente.PrecoMedio.ToString("F2");
                    txtQtde.Text = acaoExistente.Quantidade.ToString();
                    txtStop.Text = acaoExistente.StopLoss.ToString("F2");
                    txtAlvo.Text = acaoExistente.Alvo.ToString("F2");
                    chkStopAuto.Checked = false;
                }
                else
                {
                    // Nova ação: stop automático por padrão
                    chkStopAuto.Checked = true;
                }

                // Atualiza stop automático quando o preço mudar
                txtPreco.TextChanged += (s, e) =>
                {
                    if (chkStopAuto.Checked && decimal.TryParse(txtPreco.Text, out decimal preco))
                    {
                        txtStop.Text = (preco * 0.95m).ToString("F2");
                    }
                };

                btnSalvar.Click += (s, ev) =>
                {
                    if (!string.IsNullOrWhiteSpace(cmbTicker.Text) &&
                        decimal.TryParse(txtPreco.Text, out decimal preco) &&
                        int.TryParse(txtQtde.Text, out int qtde))
                    {
                        string ticker = cmbTicker.Text.ToUpper();
                        if (!ticker.EndsWith(".SA")) ticker += ".SA";

                        var existente = acoes.FirstOrDefault(a => a.Ticker == ticker);
                        decimal custoCompra = preco * qtde;

                        if (acaoExistente == null && existente == null)
                        {
                            if (custoCompra > fundosDisponiveis)
                            {
                                MessageBox.Show($"Saldo insuficiente! Disponível: R$ {fundosDisponiveis:F2}, necessário: R$ {custoCompra:F2}");
                                return;
                            }

                            fundosDisponiveis -= custoCompra;
                            SalvarFundos();
                            AtualizarFundosLabel();

                            acoes.Add(new AcaoInfo
                            {
                                Ticker = ticker,
                                PrecoMedio = preco,
                                Quantidade = qtde,
                                StopLoss = decimal.TryParse(txtStop.Text, out decimal sVal) ? sVal : (preco * 0.95m),
                                Alvo = decimal.TryParse(txtAlvo.Text, out decimal aVal) ? aVal : 0,
                                Data = DateTime.Now
                            });
                        }
                        else
                        {
                            var alvo = acaoExistente ?? existente;
                            decimal gastoExtra = custoCompra - (alvo.PrecoMedio * alvo.Quantidade);
                            if (gastoExtra > fundosDisponiveis)
                            {
                                MessageBox.Show($"Saldo insuficiente! Disponível: R$ {fundosDisponiveis:F2}, necessário: R$ {gastoExtra:F2}");
                                return;
                            }

                            fundosDisponiveis -= gastoExtra;
                            SalvarFundos();
                            AtualizarFundosLabel();

                            alvo.PrecoMedio = preco;
                            alvo.Quantidade = qtde;
                            alvo.StopLoss = decimal.TryParse(txtStop.Text, out decimal sVal2) ? sVal2 : alvo.StopLoss;
                            alvo.Alvo = decimal.TryParse(txtAlvo.Text, out decimal aVal2) ? aVal2 : alvo.Alvo;

                            if (alvo.PrecoAtual > 0)
                                alvo.Valorizacao = ((alvo.PrecoAtual - alvo.PrecoMedio) / alvo.PrecoMedio) * 100;
                        }

                        AtualizarTabela();
                        SalvarDados();
                        form.Close();
                    }
                    else
                    {
                        MessageBox.Show("Preencha corretamente Ticker, Valor e Quantidade.", "Erro");
                    }
                };

                form.Controls.Add(lblTicker);
                form.Controls.Add(cmbTicker);
                form.Controls.Add(lblPreco);
                form.Controls.Add(txtPreco);
                form.Controls.Add(lblQtde);
                form.Controls.Add(txtQtde);
                form.Controls.Add(lblStop);
                form.Controls.Add(txtStop);
                form.Controls.Add(chkStopAuto);
                form.Controls.Add(lblAlvo);
                form.Controls.Add(txtAlvo);
                form.Controls.Add(btnSalvar);

                form.ShowDialog();
            }
        }

        private async Task AtualizarValoresAsync()
        {
            if (acoes.Count == 0)
            {
                MessageBox.Show("Nenhum ticker adicionado ainda.");
                return;
            }

            try
            {
                var symbols = acoes.Select(a => a.Ticker).ToArray();
                var securities = await Yahoo.Symbols(symbols).Fields(Field.Symbol, Field.RegularMarketPrice).QueryAsync();

                foreach (var acao in acoes)
                {
                    if (securities.TryGetValue(acao.Ticker, out var data))
                    {
                        decimal precoAtual = (decimal)data.RegularMarketPrice;
                        acao.PrecoAtual = precoAtual;
                        acao.ValorAtual = precoAtual * acao.Quantidade;
                        acao.Valorizacao = ((precoAtual - acao.PrecoMedio) / acao.PrecoMedio) * 100;
                    }


                }

                AtualizarTabela();
                SalvarDados();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar dados: {ex.Message}");
            }
        }

        private void AtualizarTabela()
        {
            grid.Rows.Clear();

            decimal totalInvestido = 0;
            decimal totalGanho = 0;

            foreach (var acao in acoes)
            {
                // Atualiza ValorAtual e Valorizacao
                acao.ValorAtual = acao.PrecoAtual * acao.Quantidade;
                acao.Valorizacao = acao.PrecoAtual > 0 ? ((acao.PrecoAtual - acao.PrecoMedio) / acao.PrecoMedio) * 100 : 0;

                int idx = grid.Rows.Add(
                    acao.Ticker,
                    acao.Quantidade,
                    acao.PrecoMedio.ToString("F2"),
                    acao.PrecoAtual.ToString("F2"),
                    acao.ValorAtual.ToString("F2"),
                    acao.StopLoss.ToString("F2"),
                    "", // Coluna alerta
                    acao.Alvo.ToString("F2"),
                    acao.Valorizacao.ToString("F2") + " %",
                    acao.Data.ToShortDateString()
                );

                var row = grid.Rows[idx];
                var cellAlerta = row.Cells["alerta"];
                var cellValorizacao = row.Cells["valorizacao"];
                cellValorizacao.Style.ForeColor = acao.Valorizacao > 0 ? Color.Green :
                                                 acao.Valorizacao < 0 ? Color.Red : Color.Black;

                // Lógica de alerta
                if (acao.PrecoAtual > 0 && acao.PrecoAtual <= acao.StopLoss)
                {
                    cellAlerta.Value = "STOP";
                    cellAlerta.Style.ForeColor = Color.Red;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(117, 57, 57);
                }
                else if (acao.PrecoAtual >= acao.Alvo)
                {
                    cellAlerta.Value = "VENDER";
                    cellAlerta.Style.ForeColor = Color.DarkGreen;
                    cellAlerta.Style.Font = new Font(grid.Font, FontStyle.Bold);
                    row.DefaultCellStyle.BackColor = Color.FromArgb(67, 117, 57);
                }
                else
                {
                    cellAlerta.Value = "MANTER";
                    cellAlerta.Style.ForeColor = Color.Black;
                    cellAlerta.Style.Font = new Font(grid.Font, FontStyle.Bold);
                    row.DefaultCellStyle.BackColor = Color.White;
                }

                // Soma totals
                totalInvestido += acao.PrecoMedio * acao.Quantidade;
                totalGanho += (acao.PrecoAtual - acao.PrecoMedio) * acao.Quantidade;
            }

            // Atualiza labels
            lblTotalInvestido.Text = $"Investido: R$ {totalInvestido:F2}";
            lblGanhoPerda.Text = $"Ganho / Perda: R$ {totalGanho:F2} ({(totalInvestido > 0 ? (totalGanho / totalInvestido) * 100 : 0):F2}%)";

            lblGanhoPerda.ForeColor = totalGanho > 0 ? Color.Green : totalGanho < 0 ? Color.Red : Color.Black;

            lblFundos.Text = $"Saldo: R$ {fundosDisponiveis:F2}";
        }


        private void BtnAdicionarFundos_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Digite o valor a adicionar ao saldo:", "Adicionar saldo", "0");
            if (decimal.TryParse(input, out decimal valor) && valor > 0)
            {
                fundosDisponiveis += valor;
                AtualizarFundos();
                SalvarFundos(); // <-- salva no arquivo
            }
            else
            {
                MessageBox.Show("Valor inválido.");
            }
        }


        private void AtualizarFundos()
        {
            lblFundos.Text = $"Saldo: R$ {fundosDisponiveis:F2}";
        }

        private readonly string arquivoFundos = "fundos.json";

        private decimal CarregarFundos()
        {
            if (File.Exists(arquivoFundos))
            {
                try
                {
                    string json = File.ReadAllText(arquivoFundos);
                    return JsonSerializer.Deserialize<decimal>(json);
                }
                catch { }
            }
            return 0m;
        }

        private void SalvarFundos()
        {
            try
            {
                File.WriteAllText(arquivoFundos, JsonSerializer.Serialize(fundosDisponiveis));
            }
            catch { }
        }

        private void AtualizarFundosLabel()
        {
            lblFundos.Text = $"Saldo: R$ {fundosDisponiveis:F2}";
        }


        private class AcaoInfo
        {
            public string Ticker { get; set; }
            public int Quantidade { get; set; }
            public decimal PrecoMedio { get; set; }
            public decimal PrecoAtual { get; set; }
            public decimal ValorAtual { get; set; }
            public decimal StopLoss { get; set; }
            public decimal Alvo { get; set; }
            public decimal Valorizacao { get; set; }
            public DateTime Data { get; set; }
        }

        private class BancoDados
        {
            public List<AcaoInfo> Acoes { get; set; } = new List<AcaoInfo>();
            public decimal FundosDisponiveis { get; set; } = 0;
        }

    }
}
