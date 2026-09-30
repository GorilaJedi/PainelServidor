using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Reflection;


namespace PainelServidor
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool DrawMenuBar(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool SetActiveWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_BORDER = 0x00800000;

        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private Control focoFantasma;
        private Panel painelTopo;

        private string IP = "";
        private string USER = "";
        private string PASS = "";
        private string MAC_ADDRESS = "";
        private string UltimoCaminhoBaixar = "";
        public int QtdTerminais = 4;

        private bool telaCheiaCustom = true;
        private Rectangle tamanhoNormal;
        private bool estaArrastando = false;
        private Point ultimaPosicaoMouse;

        private TerminalPanel containerEditor;
        private string templateAtual = "4";
        private int[] distanciasSalvas = new int[3];
        private bool modoLayoutPersonalizado = false;
        private List<SplitContainer> splittersAtivos = new List<SplitContainer>();
        private Button btnConfirmarLayout;
        private Button btnCancelarLayout;
        private Button btnEditarTemplate;
        private Button btnMostrarTerminais;
        private ComboBox[] cbEditor = new ComboBox[4];
        private TerminalPanel[] paineisEditor = new TerminalPanel[4];
        private int[] layoutAtual = new int[4] { 1, 2, 3, 4 };
        private bool editandoLayout = false;
        private List<ComboBox> combosTerminais = new List<ComboBox>();
        private bool trocandoSelecao = false;
        private bool carregarEditor = true;          

        private TableLayoutPanel grid;
        private FlowLayoutPanel flowControles;
        private FlowLayoutPanel flowEditor;
        private TerminalPanel[] paineis = new TerminalPanel[4];
        private Process[] processos = new Process[4];
        public string[] COMANDOS_CUSTOMIZADOS = new string[4] { "", "", "", "" };
        public ConfigBotao[] CONFIGS_BOTOES = new ConfigBotao[3];
        public Button[] btnCustom = new Button[3];
        private Button? btnBaixarArquivo;
        private Button? btnResetServer;
        private Button? btnDesligarServer;
        private Button? btnConfigTopo;
        public Button? btnConfig;
        private Button? btnReloadTopo;
        private Button? btnSairTopo;
        private FlowLayoutPanel flowBotoes;
        public string NomeSessaoAtual = "";
        private bool conectadoComSucesso = false;
        private List<string> configsSalvas = new List<string>();
        private bool[] customComando = new bool[4]{false,false,false,false};          

        public Form1()
        {
            bool carregarSessaoDepois = false;

            this.Icon = new System.Drawing.Icon("icone.ico");

            using (FormLogin login = new FormLogin())
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    this.IP = login.IP;
                    this.USER = login.User;
                    this.PASS = login.Pass;
                    this.MAC_ADDRESS = login.MacAddress;
                    this.COMANDOS_CUSTOMIZADOS = login.ComandosCustomizados;
                    this.NomeSessaoAtual = login.SessaoSelecionada;
                    this.configsSalvas = login.ConfigsLidas;
                    this.QtdTerminais = login.QtdTerminais;
                    EnviarWakeOnLan(this.MAC_ADDRESS);
                    carregarSessaoDepois = true;
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            this.Text = "Painel Servidor" + this.NomeSessaoAtual != "" ? $" - Sessão: {this.NomeSessaoAtual}" : "";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.MinimumSize = new Size(1280, 720);

            this.MaximizedBounds = Screen.PrimaryScreen.WorkingArea;

            tamanhoNormal = new Rectangle(
                (Screen.PrimaryScreen.Bounds.Width - 1280) / 2, 
                (Screen.PrimaryScreen.Bounds.Height - 720) / 2, 
                1280, 720
            );

            this.Load += (s, e) => { 
                this.WindowState = FormWindowState.Normal;
                Screen telaAtual = Screen.FromControl(this);
                this.Bounds = new Rectangle(
                    telaAtual.Bounds.X, 
                    telaAtual.Bounds.Y, 
                    telaAtual.Bounds.Width, 
                    telaAtual.Bounds.Height - 1
                );
            };

            TableLayoutPanel layoutPrincipal = new TableLayoutPanel { 
                Dock = DockStyle.Fill, 
                ColumnCount = 1, 
                RowCount = 2,
                BackColor = Color.FromArgb(30, 30, 30),
                Margin = new Padding(0) 
            };
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 5F)); 
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 95F));

            painelTopo = new Panel { 
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 30, 30),
                Margin = new Padding(0)
            };

            flowBotoes = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                FlowDirection = FlowDirection.LeftToRight, 
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0) 
            };

            flowBotoes.VisibleChanged += (s, e) => 
            {
                if (flowBotoes.Visible)
                {
                    EventHandler executarAposDesenho = null;
                    executarAposDesenho = (sender, args) => 
                    {
                        Application.Idle -= executarAposDesenho;
                        
                        if (carregarEditor)
                        {
                            MontarEditor(this.QtdTerminais, this.templateAtual);
                            EsconderTodosOsSplitters(this.containerEditor);
                            carregarEditor = false;
                        }
                    };
                    
                    Application.Idle += executarAposDesenho;
                }
            };
            painelTopo.Controls.Add(flowBotoes);

            flowEditor = new FlowLayoutPanel { 
                Dock = DockStyle.Right,
                AutoSize = true, 
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Visible = false
            };
            painelTopo.Controls.Add(flowEditor);

            Action centralizar = () => {
                flowBotoes.Location = new Point((painelTopo.Width - flowBotoes.Width) / 2, (painelTopo.Height - flowBotoes.Height) / 2);
                flowEditor.Location = new Point((painelTopo.Width - flowEditor.Width) / 2, (painelTopo.Height - flowEditor.Height) / 2);
            };
            painelTopo.Resize += (s, e) => centralizar();
            this.Load += (s, e) => centralizar();                    

            CarregarConfiguracoesBotoes();
            for (int i = 0; i < 3; i++)
            {
                int indexBotao = i; 
                btnCustom[i] = CriarBotao(CONFIGS_BOTOES[i].Nome, CONFIGS_BOTOES[i].Cor);
                btnCustom[i].Click += (s, e) => ExecutarAcaoBotaoCustom(indexBotao);
            }

            btnResetServer = CriarBotao("REINICIAR SERVIDOR", Color.FromArgb(100, 150, 200));
            btnResetServer.Click += (s, e) => {
                if (!conectadoComSucesso) return;

                int indexPadrao = Array.IndexOf(this.COMANDOS_CUSTOMIZADOS, "");

                if (flowBotoes != null) {
                    flowBotoes.Enabled = false;
                    flowBotoes.Visible = false;
                }
                
                if (indexPadrao != -1 && processos[indexPadrao] != null) 
                {
                    EnviarComandoParaTerminal(indexPadrao, $"echo \"{PASS}\" | sudo -S systemctl reboot");                    
                }
            };

            btnBaixarArquivo = CriarBotao("BAIXAR ARQUIVO", Color.FromArgb(0, 150, 150));
            btnBaixarArquivo.Click += (s, e) => {
                if (!conectadoComSucesso) return;
                AbrirJanelaBaixarArquivo();
            };

            Padding margemPadrao = btnResetServer.Margin;
            btnBaixarArquivo.Margin = new Padding(30, margemPadrao.Top, margemPadrao.Right, margemPadrao.Bottom);
            

            btnDesligarServer = CriarBotao("DESLIGAR SERVIDOR", Color.FromArgb(200, 0, 0));
            btnDesligarServer.Click += (s, e) => {
                if (!conectadoComSucesso) return;
                int indexPadrao = Array.IndexOf(this.COMANDOS_CUSTOMIZADOS, "");

                if (flowBotoes != null) {
                    flowBotoes.Enabled = false;
                    flowBotoes.Visible = false;
                }
                
                if (indexPadrao != -1 && processos[indexPadrao] != null) 
                {
                    EnviarComandoParaTerminal(indexPadrao, $"echo \"{PASS}\" | sudo -S systemctl poweroff");
                }
            };

            flowControles = new FlowLayoutPanel { 
                Dock = DockStyle.Right,
                AutoSize = true, 
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0) 
            };
            painelTopo.Controls.Add(flowControles);

            Button btnFechar = new Button { 
                Text = "X", 
                Width = 40, 
                BackColor = Color.Red,
                Font = new Font("Arial", 9, FontStyle.Bold) ,
                TabStop = false
            };
            btnFechar.Click += (s, e) => {
                btnFechar.Enabled = false;
                btnSairTopo.Enabled = false;

                FecharTerminaisAtuais();

                var timeout = TimeSpan.FromSeconds(20);
                var inicio = DateTime.Now;

                while (VerificarSeTerminaisEstaoConectados(true))
                {
                    if (DateTime.Now - inicio > timeout)
                    {
                        break; 
                    }
                    System.Threading.Thread.Sleep(100);
                }
                this.Close();
            };
            Button btnMaximizar = new Button { 
                Text = "🗖", 
                Width = 40, 
                BackColor = Color.Gray, 
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                TabStop = false 
            };

            btnMaximizar.Click += (s, e) => {
                AlternarTelaCheia();
            };

            Button btnMinimizar = new Button { 
                Text = "-", 
                Width = 40, 
                BackColor = Color.Gray,
                Font = new Font("Arial", 15, FontStyle.Bold),
                TabStop = false
            };
            btnMinimizar.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnConfigTopo = new Button {
                Text = "⚙",
                Width = 40,
                BackColor = Color.FromArgb(70, 200, 70),
                Font = new Font("Arial", 9, FontStyle.Bold),
                TabStop = false,
                FlatStyle = FlatStyle.Flat, 
                Cursor = Cursors.Hand, 
                Padding = Padding.Empty
            };
            btnConfigTopo.FlatAppearance.BorderSize = 1; 
            btnConfigTopo.Click += BtnConfigTopo_Main_Click;
            btnConfigTopo.Margin = new Padding(margemPadrao.Left, margemPadrao.Top, 15, margemPadrao.Bottom);

            btnConfig = new Button();
            btnConfig.Click += BtnConfigTopo_Main_Click;

            btnReloadTopo = new Button {
                Text = "🔄",
                Width = 40,
                BackColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Arial", 11, FontStyle.Bold),
                TabStop = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Padding = Padding.Empty
            };
            btnReloadTopo.FlatAppearance.BorderSize = 0;
            btnReloadTopo.Click += (s, e) => { this.Invoke(new Action(() => { ReiniciarTerminaisComDelay(0, true); })); };

            btnSairTopo = new Button {
                Text = "↩",
                Width = 40,
                BackColor = Color.FromArgb(100, 50, 50),
                Font = new Font("Arial", 13, FontStyle.Bold),
                TabStop = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Padding = Padding.Empty
            };
            btnSairTopo.FlatAppearance.BorderSize = 0;
            btnSairTopo.Click += (s, e) => {
                btnFechar.Enabled = false;
                btnSairTopo.Enabled = false;

                FecharTerminaisAtuais();

                var timeout = TimeSpan.FromSeconds(20);
                var inicio = DateTime.Now;

                while (VerificarSeTerminaisEstaoConectados(true))
                {
                    if (DateTime.Now - inicio > timeout)
                    {
                        break; 
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                string caminhoDoExe = Application.ExecutablePath;

                System.Diagnostics.Process.Start(caminhoDoExe);

                Environment.Exit(0);
            };

            painelTopo.Resize += (s, e) => {
                int novaAltura = painelTopo.Height - 10;                 
                btnCustom[0].Height = novaAltura;
                btnCustom[1].Height = novaAltura;
                btnCustom[2].Height = novaAltura;
                btnBaixarArquivo.Height = novaAltura;
                btnResetServer.Height = novaAltura;
                btnDesligarServer.Height = novaAltura;
            };

            flowBotoes.Controls.Add(btnCustom[0]);
            flowBotoes.Controls.Add(btnCustom[1]);
            flowBotoes.Controls.Add(btnCustom[2]);
            flowBotoes.Controls.Add(btnBaixarArquivo);
            flowBotoes.Controls.Add(btnResetServer);
            flowBotoes.Controls.Add(btnDesligarServer);
            flowControles.Controls.Add(btnFechar);
            flowControles.Controls.Add(btnMaximizar);
            flowControles.Controls.Add(btnMinimizar);   
            flowControles.Controls.Add(btnConfigTopo);         
            flowControles.Controls.Add(btnReloadTopo);         
            flowControles.Controls.Add(btnSairTopo);
            layoutPrincipal.Controls.Add(painelTopo, 0, 0);

            grid = new TableLayoutPanel { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(0),
                Margin = new Padding(0)
            };      

            for (int i = 0; i < 4; i++) {
                paineis[i] = new TerminalPanel { 
                    Dock = DockStyle.Fill, 
                    BorderStyle = BorderStyle.None, 
                    Margin = new Padding(0),
                    Tag = i,
                    BackColor = Color.FromArgb(28 + (i *1), 28 + (i *1), 28 + (i *1))
                };
                IntPtr handleForcado = paineis[i].Handle;
            }
            
            for (int i = 0; i < 4; i++) {
                int indexTerminal = i;
                paineis[i].Resize += (s, e) => {
                    ajustarPosicaoTerminal(indexTerminal);
                };
            }

            layoutPrincipal.Controls.Add(grid, 0, 1);

            containerEditor = new TerminalPanel {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 35),
                Visible = false,
                Margin = new Padding(0)
            };

            for (int i = 0; i < 4; i++)
            {
                paineisEditor[i] = new TerminalPanel { Dock = DockStyle.Fill, Margin = new Padding(5), BackColor = Color.FromArgb(60, 60, 60), BorderStyle = BorderStyle.FixedSingle };
                cbEditor[i] = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Tag = i, Width = 150, Font = new Font("Arial", 14, FontStyle.Bold) };
                cbEditor[i].Items.AddRange(new object[] { "Terminal 1", "Terminal 2", "Terminal 3", "Terminal 4" });
                cbEditor[i].SelectedIndex = i;
                cbEditor[i].DropDown += CbEditor_DropDown;
                cbEditor[i].SelectedIndexChanged += CbEditor_SelectedIndexChanged;
                paineisEditor[i].Controls.Add(cbEditor[i]);

                ComboBox cbCopia = cbEditor[i];
                paineisEditor[i].Resize += (s, e) => { cbCopia.Location = new Point((((Panel)s).Width - cbCopia.Width) / 2, (((Panel)s).Height - cbCopia.Height) / 2); };
            }

            layoutPrincipal.Controls.Add(containerEditor, 0, 1);
            containerEditor.BringToFront();

            btnCancelarLayout = new Button {
                Text = "✖ CANCELAR LAYOUT", Width = 180, BackColor = Color.FromArgb(150, 0, 0), ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold), Visible = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 10, 15, 10)
            };
            btnCancelarLayout.Click += (s, e) => {
                editandoLayout = false;
                containerEditor.Visible = false;
                flowEditor.Visible = false;
                flowBotoes.Visible = true;
                grid.Visible = true;
                this.Invoke(new Action(() => {
                    CarregarConfiguracoesSessao();
                }));
                AtualizarLayoutGrid();
                btnConfigTopo.PerformClick();
            };
            flowEditor.Controls.Add(btnCancelarLayout);

            btnConfirmarLayout = new Button {
                Text = "✔ APLICAR LAYOUT", Width = 180, BackColor = Color.FromArgb(0, 150, 0), ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold), Visible = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 10, 15, 10)
            };
            btnConfirmarLayout.Click += (s, e) => {
                for (int i = 0; i < splittersAtivos.Count && i < 3; i++) {
                    distanciasSalvas[i] = splittersAtivos[i].SplitterDistance;
                }

                modoLayoutPersonalizado = true;                

                editandoLayout = false;
                containerEditor.Visible = false;
                flowEditor.Visible = false;
                flowBotoes.Visible = true;
                grid.Visible = true;

                string layoutStr = "";
                layoutStr = $"{templateAtual};{layoutAtual[0]},{layoutAtual[1]},{layoutAtual[2]},{layoutAtual[3]};{distanciasSalvas[0]},{distanciasSalvas[1]},{distanciasSalvas[2]}";
                while (this.configsSalvas.Count <= 14)
                {
                    this.configsSalvas.Add(""); 
                }
                this.configsSalvas[14] = layoutStr;

                if (!string.IsNullOrWhiteSpace(this.NomeSessaoAtual))
                {
                    try
                    {
                        string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
                        string caminhoCompleto = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

                        if (File.Exists(caminhoCompleto))
                        {
                            SalvarLinhaNoArquivo(LinhasArquivo.QtdTerminais, this.QtdTerminais.ToString());                            
                            SalvarLinhaNoArquivo(LinhasArquivo.Template, layoutStr);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show($"Erro ao gravar comandos: {ex.Message}"); }
                }

                AtualizarLayoutGrid();

            };
            flowEditor.Controls.Add(btnConfirmarLayout);

            btnEditarTemplate = new Button {
                Text = "◫ Editar Template", Width = 180, BackColor = Color.FromArgb(0, 0, 150), ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold), Visible = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 10, 15, 10)
            };
            btnEditarTemplate.Click += (s, e) => {
                if (this.QtdTerminais == 2 || this.QtdTerminais == 3)
                {
                    using (FormEscolhaTemplate popupEscolha = new FormEscolhaTemplate(this.QtdTerminais))
                    {
                        if (popupEscolha.ShowDialog() == DialogResult.OK) this.templateAtual = popupEscolha.TemplateEscolhido;
                        else return;
                    }
                }
                distanciasSalvas = new int[3];

                MontarEditor(this.QtdTerminais, this.templateAtual);
            };
            flowEditor.Controls.Add(btnEditarTemplate);

            btnMostrarTerminais = new Button {
                Text = "Mostrar Terminais", Width = 180, BackColor = Color.FromArgb(100, 100, 100), ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold), Visible = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 10, 15, 10)
            };

            btnMostrarTerminais.Click += (s, e) => {
                bool deveExibir = btnMostrarTerminais.Text.Trim() == "Mostrar Terminais";

                for (int i = 0; i < 4; i++) 
                {
                    int idTerminal = layoutAtual[i];
                    if (idTerminal > 0 && idTerminal <= 4) 
                    {
                        Control terminalReal = paineis[idTerminal - 1];
                        
                        if (deveExibir)
                        {
                            if (terminalReal.Parent != paineisEditor[i]) 
                            {
                                if (terminalReal.Parent != null) terminalReal.Parent.Controls.Remove(terminalReal);
                                
                                terminalReal.Dock = DockStyle.Fill;
                                paineisEditor[i].Controls.Add(terminalReal);
                            }
                            
                            terminalReal.Visible = true;
                            terminalReal.BringToFront(); 
                        }
                        else
                        {
                            terminalReal.Visible = false;
                        }
                    }
                }

                btnMostrarTerminais.Text = deveExibir ? "Ocultar Terminais" : "Mostrar Terminais";

                if (deveExibir)
                {
                    this.BeginInvoke(new Action(() => {
                        for (int i = 0; i < 4; i++) 
                        {
                            ajustarPosicaoTerminal(i);
                        }
                    }));
                }
            };

            flowEditor.Controls.Add(btnMostrarTerminais);

            this.Controls.Add(layoutPrincipal);

            if (flowBotoes != null) {
                flowBotoes.Enabled = false;
                flowBotoes.Visible = false;
            }
            if (btnReloadTopo != null) btnReloadTopo.Enabled = false;

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            foreach (Control ctrl in painelTopo.Controls)
            {
                ctrl.MouseDown += (s, e) => {
                    if (e.Button == MouseButtons.Left)
                    {
                        estaArrastando = true;
                        ultimaPosicaoMouse = Cursor.Position;
                    }
                };

                ctrl.MouseMove += (s, e) => {
                    if (estaArrastando && e.Button == MouseButtons.Left)
                    {
                        if (Math.Abs(Cursor.Position.X - ultimaPosicaoMouse.X) > 3 || 
                            Math.Abs(Cursor.Position.Y - ultimaPosicaoMouse.Y) > 3)
                        {
                            
                            estaArrastando = false; 

                            ((Control)s).Capture = false;

                            if (telaCheiaCustom)
                            {
                                double proporcaoX = (double)e.X / this.Width;
                                double proporcaoY = (double)e.Y / this.Height;

                                AlternarTelaCheia();

                                int novoOffsetProporcional = (int)(this.Width * proporcaoX);
                                int novoOffsetProporcionalY = (int)(this.Height * proporcaoY);

                                this.Location = new Point(Cursor.Position.X - novoOffsetProporcional, Cursor.Position.Y - novoOffsetProporcionalY);
                            }
                            ReleaseCapture();
                            SendMessage(this.Handle, 0xA1, 0x2, 0);
                        }
                    }
                };

                ctrl.MouseUp += (s, e) => {
                    estaArrastando = false;
                };
            }

            painelTopo.MouseDown += (s, e) => {
                estaArrastando = true;
                ultimaPosicaoMouse = Cursor.Position;
                
                ReleaseCapture();
                SendMessage(this.Handle, 0x112, 0xf012, 0);
            };

            painelTopo.MouseMove += (s, e) => {
                if (estaArrastando && telaCheiaCustom)
                {
                    if (Math.Abs(Cursor.Position.X - ultimaPosicaoMouse.X) > 3 || 
                        Math.Abs(Cursor.Position.Y - ultimaPosicaoMouse.Y) > 3)
                    {
                        AlternarTelaCheia();
                        
                        estaArrastando = false; 
                    }
                }
            };

            painelTopo.MouseUp += (s, e) => {
                estaArrastando = false;
            };

            focoFantasma = new Control { Size = new Size(0, 0), Visible = true };
            this.Controls.Add(focoFantasma);

            this.Shown += (s, e) =>
            {
                if (carregarSessaoDepois)
                {
                    this.Invoke(new Action(() => {
                        CarregarConfiguracoesSessao();
                    }));

                    if (this.modoLayoutPersonalizado && containerEditor != null)
                    {
                        if (!this.Controls.Contains(containerEditor))
                        {
                            this.Controls.Add(containerEditor);
                        }
                        containerEditor.Dock = DockStyle.Fill;
                        containerEditor.Visible = true;
                        containerEditor.BringToFront();
                        Application.DoEvents();
                    }

                    AtualizarLayoutGrid();
                }
                AtualizarVisibilidadeBotoesServidor();

                new Thread(() => this.Invoke(new Action(() => 
                { 
                    ReiniciarTerminaisComDelay(500, true); 
                }))) { IsBackground = true }.Start();
            };
            
        }

        public async void ExecutarReiniciarTerminal(int termIndex)
        {
            customComando[termIndex] = false;
            if (processos[termIndex] != null && !processos[termIndex].HasExited)
            {
                try 
                { 
                    processos[termIndex].Kill(); 
                    processos[termIndex].WaitForExit(); 
                } 
                catch { }
            }      
            
            await System.Threading.Tasks.Task.Delay(50);          
            
            AbrirTerminal(termIndex);
            AtualizarVisibilidadeBotoesServidor();
        }

        private int GetTerminalId(object item)
        {
            if (item == null) return 1;
            string s = item.ToString();
            
            if (int.TryParse(s, out int numero)) return numero;
            
            if (s.Contains("Terminal "))
            {
                return int.Parse(s.Replace("Terminal ", ""));
            }
            return 1;
        }

        private void CbEditor_DropDown(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            string valorAtualStr = cb.SelectedItem?.ToString();
            
            cb.Items.Clear();
            
            for (int tId = 1; tId <= 4; tId++)
            {
                cb.Items.Add($"Terminal {tId}");
            }
            
            cb.SelectedItem = valorAtualStr;
        }

        private void CbEditor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (trocandoSelecao) return;

            ComboBox cbAtual = sender as ComboBox;
            if (cbAtual == null || cbAtual.SelectedItem == null) return;

            int painelAtualIndex = (int)cbAtual.Tag;
            int novoTerminalId = GetTerminalId(cbAtual.SelectedItem); 
            int antigoTerminalId = layoutAtual[painelAtualIndex];

            if (novoTerminalId == antigoTerminalId) return;

            trocandoSelecao = true;

            try
            {
                for (int i = 0; i < layoutAtual.Length; i++)
                {
                    if (i != painelAtualIndex && layoutAtual[i] == novoTerminalId)
                    {
                        layoutAtual[i] = antigoTerminalId;

                        Control[] controlesEncontrados = this.Controls.Find($"cb_painel_{i}", true); 
                        
                        if (controlesEncontrados.Length == 0)
                        {
                            BuscarEAtualizarComboBoxPorTag(this, i, antigoTerminalId);
                        }
                        else if (controlesEncontrados[0] is ComboBox outroCb)
                        {
                            AtualizarTextoComboPorId(outroCb, antigoTerminalId);
                        }

                        break;
                    }
                }

                layoutAtual[painelAtualIndex] = novoTerminalId;
            }
            finally
            {
                trocandoSelecao = false;
            }

            this.BeginInvoke(new Action(() => {
                for (int i = 0; i < 4; i++) 
                {
                    ajustarPosicaoTerminal(i);
                }
            }));
        }

        private void BuscarEAtualizarComboBoxPorTag(Control pai, int tagAlvo, int novoTerminalId)
        {
            foreach (Control c in pai.Controls)
            {
                if (c is ComboBox cb && cb.Tag != null && (int)cb.Tag == tagAlvo)
                {
                    AtualizarTextoComboPorId(cb, novoTerminalId);
                    return;
                }
                if (c.HasChildren)
                {
                    BuscarEAtualizarComboBoxPorTag(c, tagAlvo, novoTerminalId);
                }
            }
        }

        private void AtualizarTextoComboPorId(ComboBox cb, int terminalId)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (GetTerminalId(cb.Items[i]) == terminalId)
                {
                    cb.SelectedIndex = i;
                    break;
                }
            }
        }

        private void MontarEditor(int qtdTerminais, string tipoTemplate)
        {
            this.templateAtual = tipoTemplate;
            containerEditor.Controls.Clear();
            splittersAtivos.Clear();
            combosTerminais.Clear();

            for (int i = 0; i < 4; i++) paineisEditor[i].Visible = false;

            if (qtdTerminais == 1)
            {
                paineisEditor[0].Visible = true;
                containerEditor.Controls.Add(paineisEditor[0]);
            }
            else if (qtdTerminais == 2)
            {
                SplitContainer split = new SplitContainer { Orientation = tipoTemplate == "2V" ? Orientation.Vertical : Orientation.Horizontal, Dock = DockStyle.Fill, SplitterWidth = 8 };
                split.Panel1.Controls.Add(paineisEditor[0]); paineisEditor[0].Visible = true;
                split.Panel2.Controls.Add(paineisEditor[1]); paineisEditor[1].Visible = true;
                containerEditor.Controls.Add(split);
                splittersAtivos.Add(split);
            }
            else if (qtdTerminais == 3)
            {
                SplitContainer splitRoot = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = 8 };
                SplitContainer splitFilho = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = 8 };
                if (tipoTemplate == "3E") { splitRoot.Orientation = Orientation.Vertical; splitFilho.Orientation = Orientation.Horizontal; splitRoot.Panel1.Controls.Add(paineisEditor[0]); splitRoot.Panel2.Controls.Add(splitFilho); splitFilho.Panel1.Controls.Add(paineisEditor[1]); splitFilho.Panel2.Controls.Add(paineisEditor[2]); }
                else if (tipoTemplate == "3D") { splitRoot.Orientation = Orientation.Vertical; splitFilho.Orientation = Orientation.Horizontal; splitRoot.Panel1.Controls.Add(splitFilho); splitRoot.Panel2.Controls.Add(paineisEditor[2]); splitFilho.Panel1.Controls.Add(paineisEditor[0]); splitFilho.Panel2.Controls.Add(paineisEditor[1]); }
                else if (tipoTemplate == "3T") { splitRoot.Orientation = Orientation.Horizontal; splitFilho.Orientation = Orientation.Vertical; splitRoot.Panel1.Controls.Add(paineisEditor[0]); splitRoot.Panel2.Controls.Add(splitFilho); splitFilho.Panel1.Controls.Add(paineisEditor[1]); splitFilho.Panel2.Controls.Add(paineisEditor[2]); }
                else if (tipoTemplate == "3B") { splitRoot.Orientation = Orientation.Horizontal; splitFilho.Orientation = Orientation.Vertical; splitRoot.Panel1.Controls.Add(splitFilho); splitRoot.Panel2.Controls.Add(paineisEditor[2]); splitFilho.Panel1.Controls.Add(paineisEditor[0]); splitFilho.Panel2.Controls.Add(paineisEditor[1]); }
                for (int i = 0; i < 3; i++) paineisEditor[i].Visible = true;
                containerEditor.Controls.Add(splitRoot);
                splittersAtivos.Add(splitRoot);
                splittersAtivos.Add(splitFilho);
            }
            else if (qtdTerminais == 4)
            {
                SplitContainer splitV = new SplitContainer { Orientation = Orientation.Vertical, Dock = DockStyle.Fill, SplitterWidth = 8 };
                SplitContainer splitH1 = new SplitContainer { Orientation = Orientation.Horizontal, Dock = DockStyle.Fill, SplitterWidth = 8 };
                SplitContainer splitH2 = new SplitContainer { Orientation = Orientation.Horizontal, Dock = DockStyle.Fill, SplitterWidth = 8 };
                splitV.Panel1.Controls.Add(splitH1); splitV.Panel2.Controls.Add(splitH2);
                splitH1.Panel1.Controls.Add(paineisEditor[0]); splitH1.Panel2.Controls.Add(paineisEditor[2]);
                splitH2.Panel1.Controls.Add(paineisEditor[1]); splitH2.Panel2.Controls.Add(paineisEditor[3]);
                for (int i = 0; i < 4; i++) paineisEditor[i].Visible = true;
                containerEditor.Controls.Add(splitV);
                splittersAtivos.Add(splitV);
                splittersAtivos.Add(splitH1);
                splittersAtivos.Add(splitH2);
            }

            this.BeginInvoke(new Action(() => 
            {
                int index = 0;
                
                foreach (Control c1 in containerEditor.Controls)
                {
                    if (c1 is SplitContainer splitPrincipal)
                    {
                        if (index < distanciasSalvas.Length && distanciasSalvas[index] > 0)
                        {
                            try { splitPrincipal.SplitterDistance = distanciasSalvas[index]; } catch { }
                        }
                        index++;

                        foreach (Control c2 in splitPrincipal.Panel1.Controls)
                        {
                            if (c2 is SplitContainer splitEsq)
                            {
                                if (index < distanciasSalvas.Length && distanciasSalvas[index] > 0)
                                {
                                    try { splitEsq.SplitterDistance = distanciasSalvas[index]; } catch { }
                                }
                                index++;
                            }
                        }

                        foreach (Control c3 in splitPrincipal.Panel2.Controls)
                        {
                            if (c3 is SplitContainer splitDir)
                            {
                                if (index < distanciasSalvas.Length && distanciasSalvas[index] > 0)
                                {
                                    try { splitDir.SplitterDistance = distanciasSalvas[index]; } catch { }
                                }
                                index++;
                            }
                        }
                    }
                }
            }));
        }

        private void EsconderTodosOsSplitters(Control controlePai)
        {
            if (controlePai == null || !controlePai.HasChildren) return; 

            foreach (Control ctrl in controlePai.Controls)
            {
                if (ctrl is SplitContainer split)
                {
                    split.BorderStyle = BorderStyle.None;
                    split.IsSplitterFixed = true;
                    split.SplitterWidth = 1;
                    
                    EsconderTodosOsSplitters(split.Panel1);
                    EsconderTodosOsSplitters(split.Panel2);
                }
                else if (ctrl.HasChildren)
                {
                    EsconderTodosOsSplitters(ctrl);
                }
            }
        }

        private void AlternarTelaCheia()
        {
            this.WindowState = FormWindowState.Normal; 

            if (telaCheiaCustom)
            {
                Screen telaAtual = Screen.FromControl(this);
                
                Screen telaAntiga = Screen.FromRectangle(tamanhoNormal);

                if (telaAtual.DeviceName != telaAntiga.DeviceName)
                {
                    int offsetX = telaAtual.Bounds.X - telaAntiga.Bounds.X;
                    int offsetY = telaAtual.Bounds.Y - telaAntiga.Bounds.Y;
                    
                    tamanhoNormal = new Rectangle(
                        tamanhoNormal.X + offsetX, 
                        tamanhoNormal.Y + offsetY, 
                        tamanhoNormal.Width, 
                        tamanhoNormal.Height
                    );
                }

                this.Bounds = tamanhoNormal;
                telaCheiaCustom = false;
            }
            else
            {
                tamanhoNormal = this.Bounds;
                Screen telaAtual = Screen.FromControl(this);
                
                this.Bounds = new Rectangle(
                    telaAtual.Bounds.X, 
                    telaAtual.Bounds.Y, 
                    telaAtual.Bounds.Width, 
                    telaAtual.Bounds.Height - 1
                );
                
                telaCheiaCustom = true;
            }
        }

        public void AtualizarLayoutGrid()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(AtualizarLayoutGrid));
                return;
            }

            grid.SuspendLayout();

            grid.Controls.Clear();
            grid.RowStyles.Clear();
            grid.ColumnStyles.Clear();

            if (modoLayoutPersonalizado)
            {
                grid.Controls.Clear();

                EsconderTodosOsSplitters(containerEditor);

                foreach (Control ctrl in containerEditor.Controls)
                {
                    if (ctrl is SplitContainer split)
                    {
                        split.BorderStyle = BorderStyle.None;
                        split.IsSplitterFixed = true; 
                        split.SplitterWidth = 1;      
                    }
                }
                
                for (int i = 0; i < 4; i++) 
                {
                    paineisEditor[i].Controls.Clear();
                    paineisEditor[i].Visible = (i < 4);
                }

                for (int i = 0; i < 4; i++) 
                {
                    int idTerminal = layoutAtual[i];
                    if (idTerminal > 0 && idTerminal <= 4) 
                    {
                        Control terminalReal = paineis[idTerminal - 1];
                        
                        if (terminalReal.Parent != null) terminalReal.Parent.Controls.Remove(terminalReal);
                        
                        terminalReal.Dock = DockStyle.Fill;
                        terminalReal.Visible = true;
                        paineisEditor[i].Controls.Add(terminalReal);
                    }
                }

                containerEditor.Dock = DockStyle.Fill;
                grid.Controls.Add(containerEditor);
                grid.ColumnCount = 1;
                grid.RowCount = 1;

                containerEditor.Visible = true;
                containerEditor.BringToFront();

                foreach(int id in layoutAtual)
                {
                    if(id > 0 && id <= 4) paineis[id-1].Visible = true;
                    ajustarPosicaoTerminal(id-1);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++) {
                    if (paineis[i] != null) grid.SetColumnSpan(paineis[i], 1);
                }

                if (this.QtdTerminais == 1)
                {
                    grid.ColumnCount = 1; grid.RowCount = 1;
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    grid.Controls.Add(paineis[0], 0, 0);
                }
                else if (this.QtdTerminais == 2)
                {
                    grid.ColumnCount = 2; grid.RowCount = 1;
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    grid.Controls.Add(paineis[0], 0, 0);
                    grid.Controls.Add(paineis[1], 1, 0);
                }
                else if (this.QtdTerminais == 3)
                {
                    grid.ColumnCount = 2; grid.RowCount = 2;
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                    grid.Controls.Add(paineis[0], 0, 0);
                    grid.Controls.Add(paineis[1], 1, 0);
                    grid.Controls.Add(paineis[2], 0, 1);
                    grid.SetColumnSpan(paineis[2], 2);
                }
                else if (this.QtdTerminais == 4)
                {
                    grid.ColumnCount = 2; grid.RowCount = 2;
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                    grid.Controls.Add(paineis[0], 0, 0);
                    grid.Controls.Add(paineis[1], 1, 0);
                    grid.Controls.Add(paineis[2], 0, 1);
                    grid.Controls.Add(paineis[3], 1, 1);
                }

                for (int i = 0; i < 4; i++) {
                    if (paineis[i] != null) paineis[i].Visible = (i < this.QtdTerminais);
                }
            }         

            grid.ResumeLayout(true);
        }

        public static void EnviarWakeOnLan(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return;
            string macLimpo = Regex.Replace(macAddress, "[-|:| ]", "");
            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++) macBytes[i] = Convert.ToByte(macLimpo.Substring(i * 2, 2), 16);

            byte[] packet = new byte[6 + 16 * 6];
            for (int i = 0; i < 6; i++) packet[i] = 0xFF;
            for (int i = 1; i <= 16; i++) Array.Copy(macBytes, 0, packet, i * 6, 6);

            using (UdpClient client = new UdpClient())
            {
                try { client.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9)); } catch { }

                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                byte[] ipBytes = ip.Address.GetAddressBytes();
                                byte[] maskBytes = ip.IPv4Mask.GetAddressBytes();
                                byte[] broadcast = new byte[4];
                                for (int i = 0; i < 4; i++) broadcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                                
                                try { client.Send(packet, packet.Length, new IPEndPoint(new IPAddress(broadcast), 9)); } catch { }
                            }
                        }
                    }
                }
            }
        }

        private void AtualizarVisibilidadeBotoesServidor()
        {
            if (btnBaixarArquivo == null || btnResetServer == null || btnDesligarServer == null) return;
            bool temTerminalPadrao = false;
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrWhiteSpace(this.COMANDOS_CUSTOMIZADOS[i]) && customComando[i] == false)
                {
                    temTerminalPadrao = true;
                    break;
                }
            }
            btnResetServer.Visible = temTerminalPadrao;
            btnDesligarServer.Visible = temTerminalPadrao;
            btnBaixarArquivo.Visible = temTerminalPadrao;
        }

        private void BtnConfigTopo_Main_Click(object? sender, EventArgs e)
        {
            string sessaoExibida = !string.IsNullOrWhiteSpace(this.NomeSessaoAtual) ? this.NomeSessaoAtual : this.configsSalvas.Count > 0 ? this.configsSalvas[0] : "Configurações Padrão";
            using (FormPopupComandos popup = new FormPopupComandos(sessaoExibida, this.COMANDOS_CUSTOMIZADOS, this.MAC_ADDRESS, this.QtdTerminais))
            {
                popup.DialogResult = DialogResult.None;

                DialogResult res = popup.ShowDialog();

                if (res == DialogResult.Retry) 
                {
                    int qtdEscolhida = popup.QtdTerminaisRespondido;
                    string templateEscolhido = "4"; 

                    if (modoLayoutPersonalizado && qtdEscolhida == this.QtdTerminais) 
                    {
                        templateEscolhido = this.templateAtual;
                    } 
                    else 
                    {
                        if (qtdEscolhida == 1) templateEscolhido = "1";
                        else if (qtdEscolhida == 2 || qtdEscolhida == 3)
                        {
                            using (FormEscolhaTemplate popupEscolha = new FormEscolhaTemplate(qtdEscolhida))
                            {
                                if (popupEscolha.ShowDialog() == DialogResult.OK) templateEscolhido = popupEscolha.TemplateEscolhido;
                                else return;
                            }
                        }
                        distanciasSalvas = new int[3];
                    }
                    

                    MontarEditor(qtdEscolhida, templateEscolhido);

                    for (int i = 0; i < 4; i++) {
                        paineisEditor[i].Controls.Clear();
                        paineisEditor[i].BorderStyle = BorderStyle.FixedSingle;
                        cbEditor[i].SelectedIndex = (layoutAtual[i] > 0 && layoutAtual[i] <= 4) ? layoutAtual[i] - 1 : i; 
                        paineisEditor[i].Controls.Add(cbEditor[i]);
                        cbEditor[i].Location = new Point((paineisEditor[i].Width - cbEditor[i].Width) / 2, (paineisEditor[i].Height - cbEditor[i].Height) / 2);
                    }

                    if (containerEditor.Parent == grid)
                    {
                        TableLayoutPanel layoutP = grid.Parent as TableLayoutPanel;
                        if (layoutP != null) layoutP.Controls.Add(containerEditor, 0, 1);
                        else grid.Parent.Controls.Add(containerEditor);
                    }
                    editandoLayout = true;
                    containerEditor.Visible = true;
                    containerEditor.BringToFront();
                    flowEditor.Visible = true;
                    flowBotoes.Visible = false;
                    grid.Visible = false;

                    this.QtdTerminais = popup.QtdTerminaisRespondido;
                    
                }       
                else if (res == DialogResult.OK)
                {
                    this.COMANDOS_CUSTOMIZADOS = popup.ComandosRespondidos;
                    this.MAC_ADDRESS = popup.MacRespondido;

                    bool layoutMudou = (this.QtdTerminais != popup.QtdTerminaisRespondido);
                    this.QtdTerminais = popup.QtdTerminaisRespondido;
                    
                    if (layoutMudou)
                    {
                        modoLayoutPersonalizado = false;
                        AtualizarLayoutGrid();                                                
                        this.Invoke(new Action(() => { ReiniciarTerminaisComDelay(500, true); }));
                    }

                    if (!string.IsNullOrWhiteSpace(this.NomeSessaoAtual))
                    {
                        try
                        {
                            string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
                            string caminhoCompleto = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

                            if (File.Exists(caminhoCompleto))
                            {
                                for (int i = LinhasArquivo.FinalCmdInicial; i >= LinhasArquivo.InicioCmdInicial; i--) SalvarLinhaNoArquivo(LinhasArquivo.FinalCmdInicial-i+LinhasArquivo.InicioCmdInicial, this.COMANDOS_CUSTOMIZADOS[LinhasArquivo.FinalCmdInicial-i]);
                                SalvarLinhaNoArquivo(LinhasArquivo.MAC, this.MAC_ADDRESS);
                                SalvarLinhaNoArquivo(LinhasArquivo.QtdTerminais, this.QtdTerminais.ToString());
                                
                                string layoutStr = "";
                                if (modoLayoutPersonalizado) {
                                    layoutStr = $"{templateAtual};{layoutAtual[0]},{layoutAtual[1]},{layoutAtual[2]},{layoutAtual[3]};{distanciasSalvas[0]},{distanciasSalvas[1]},{distanciasSalvas[2]}";
                                }
                                SalvarLinhaNoArquivo(LinhasArquivo.Template, layoutStr);
                            }
                        }
                        catch (Exception ex) { MessageBox.Show($"Erro ao gravar comandos: {ex.Message}"); }
                    }
                    AtualizarVisibilidadeBotoesServidor();
                }                   
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!conectadoComSucesso) return;
            switch (e.KeyCode)
            {       
                case Keys.F1: btnCustom[0]?.PerformClick(); break;
                case Keys.F2: btnCustom[1]?.PerformClick(); break;
                case Keys.F3: btnCustom[2]?.PerformClick(); break;
                case Keys.F4: btnBaixarArquivo?.PerformClick(); break;
                case Keys.F5: btnResetServer?.PerformClick(); break;
                case Keys.F6: btnDesligarServer?.PerformClick(); break;
            }
        }

        private void Inicializar(int delay = 125) { 
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Inicializar(delay)));
                return;
            }

            if (flowBotoes != null) {
                flowBotoes.Enabled = false;   
                flowBotoes.Visible = false; 
            }
            if (btnReloadTopo != null) btnReloadTopo.Enabled = false;

            if (ServidorEstaOnline(this.IP, 22)) {
                System.Threading.Thread.Sleep(delay);
                for (int i = 0; i < 4; i++){
                    AbrirTerminal(i);
                    customComando[i] = false;
                }
                Application.DoEvents();
                if (flowBotoes != null) {
                    flowBotoes.Enabled = true;
                    flowBotoes.Visible = true;
                }
                if (btnReloadTopo != null) btnReloadTopo.Enabled = true;
                this.Invoke(new Action(() => {
                    if (painelTopo != null) painelTopo.BringToFront(); 
                    this.Activate(); 
                }));
            }
            else this.Invoke(new Action(() => { ReiniciarTerminaisComDelay(500, true); }));
        }

        private void AbrirTerminal(int i)
        {
            paineis[i].Controls.Clear(); 
            
            string comandoExecutar = "";
            if (!string.IsNullOrWhiteSpace(COMANDOS_CUSTOMIZADOS[i]))
            {
                comandoExecutar = COMANDOS_CUSTOMIZADOS[i].Trim();
            }

            if (comandoExecutar.StartsWith(":"))
            {
                string porta = comandoExecutar.Substring(1).Trim();
                string url = $"http://{this.IP}:{porta}";

                var navegador = new Microsoft.Web.WebView2.WinForms.WebView2 {
                    Dock = DockStyle.Fill
                };

                navegador.CoreWebView2InitializationCompleted += (sender, args) =>
                {
                    if (args.IsSuccess)
                    {
                        navegador.CoreWebView2.Settings.IsStatusBarEnabled = false;
                        navegador.CoreWebView2.ContainsFullScreenElementChanged += (s, e) =>
                        {
                            if (navegador.CoreWebView2.ContainsFullScreenElement)
                            {
                                navegador.Parent = this;
                                navegador.BringToFront();
                            }
                            else
                            {
                                navegador.Parent = paineis[i];
                            }
                        };
                    }
                };

                paineis[i].Controls.Add(navegador);
                navegador.Source = new Uri(url);
                
                return; 
            }

            string puttyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "putty.exe");
            string args = $"-ssh {USER}@{IP} -pw {PASS} -t"; 
            if (!string.IsNullOrWhiteSpace(COMANDOS_CUSTOMIZADOS[i]))
            {
                if (COMANDOS_CUSTOMIZADOS[i].StartsWith("sudo "))
                    {
                        comandoExecutar = $"echo \"{{PASS}}\" | sudo -S {COMANDOS_CUSTOMIZADOS[i].Substring(5)}";
                    }
                comandoExecutar = string.Join(" ; ", comandoExecutar).Replace("{PASS}", this.PASS);
            }
            else comandoExecutar = "";



            if (!string.IsNullOrEmpty(comandoExecutar)) args += $" -m \"{CriarScriptTemp(comandoExecutar)}\"";

            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64).CreateSubKey(@"Software\SimonTatham\PuTTY\Sessions\Default%20Settings"))
                {
                    if (key != null)
                    {
                        key.SetValue("CloseOnExit", 2, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            if (processos[i] != null && !processos[i].HasExited)
            {
                processos[i].Kill(true);
                processos[i].WaitForExit(50);
            }

            processos[i] = Process.Start(puttyPath, args);
            
            int tentativas = 0;
            while (processos[i].MainWindowHandle == IntPtr.Zero && tentativas < 50) { 
                Thread.Sleep(100); 
                processos[i].Refresh(); 
                tentativas++;
            }

            if (processos[i].MainWindowHandle != IntPtr.Zero)
            {
                System.Threading.Thread.Sleep(500);
                SetParent(processos[i].MainWindowHandle, paineis[i].Handle);
                int WS_CHILD = 0x40000000;
                int WS_VISIBLE = 0x10000000;
                SetWindowLong(processos[i].MainWindowHandle, GWL_STYLE, WS_CHILD | WS_VISIBLE);
                SetWindowPos(processos[i].MainWindowHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);

                ajustarPosicaoTerminal(i);
            }
        }

        private void ajustarPosicaoTerminal(int i)
        {
            int offset = 9;

            if (processos[i] == null || processos[i].HasExited) return;
            IntPtr hWndPuTTY = processos[i].MainWindowHandle;
            if (hWndPuTTY == IntPtr.Zero) return;

            IntPtr handleAtual = paineis[i].Handle;
            if (handleAtual != IntPtr.Zero)
            {
                SetParent(hWndPuTTY, handleAtual);
            }

            int style = GetWindowLong(hWndPuTTY, GWL_STYLE);
            SetWindowLong(hWndPuTTY, GWL_STYLE, style & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_BORDER);

            MoveWindow(hWndPuTTY, -offset+2, -offset*3, paineis[i].Width + (offset * 2), paineis[i].Height + (offset * 4), true);
        }

        private string CriarScriptTemp(string comando)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "cmd.txt");
            File.WriteAllText(tempFile, comando);
            return tempFile;
        }

        private async void EnviarComandoParaTerminal(int index, string comando)
        {
            string comandoCustom = COMANDOS_CUSTOMIZADOS[index]?.Trim() ?? "";
            string comandoDigitado = comando.Trim();

            if (comandoDigitado.StartsWith("acessar") && comandoDigitado.Contains(":"))
            {
                int indexDoisPontos = comandoDigitado.IndexOf(":");
                string porta = comandoDigitado.Substring(indexDoisPontos + 1).Trim();

                COMANDOS_CUSTOMIZADOS[index] = $":{porta}";

                ExecutarReiniciarTerminal(index);

                AtualizarVisibilidadeBotoesServidor();
                
                await System.Threading.Tasks.Task.Delay(500);

                COMANDOS_CUSTOMIZADOS[index] = comandoCustom;

                customComando[index] = true;
                
                return; 
            }
            else if (!comandoCustom.StartsWith(":"))
            {
                if (processos[index] != null && !processos[index].HasExited)
                {
                    FocarTerminal(index);
                    SendKeys.SendWait("^c"+"{ENTER}");
                    Thread.Sleep(500);
                    StringBuilder comandoTratado = new StringBuilder();
                    foreach (char c in comando)
                    {
                        switch (c)
                        {
                            case '{': comandoTratado.Append("{{}"); break;
                            case '}': comandoTratado.Append("{}}"); break;
                            case '/': comandoTratado.Append("{DIVIDE}"); break;
                            case '*': comandoTratado.Append("{MULTIPLY}"); break;
                            case '-': comandoTratado.Append("{SUBTRACT}"); break;
                            case '+': comandoTratado.Append("{ADD}"); break;
                            case '^': comandoTratado.Append("{^}"); break;
                            case '%': comandoTratado.Append("{%}"); break;
                            case '~': comandoTratado.Append("{~}"); break;
                            case '(': comandoTratado.Append("{(}"); break;
                            case ')': comandoTratado.Append("{)}"); break;
                            case '[': comandoTratado.Append("{[}"); break;
                            case ']': comandoTratado.Append("{]}"); break;
                            default:  comandoTratado.Append(c); break;
                        }
                    }
                    SendKeys.SendWait(comandoTratado+"{ENTER}");
                }
            }
        }

        public void RecarregarVisualEBotoes()
        {
            for (int i = 0; i < 3; i++)
            {
                if (btnCustom[i] != null && CONFIGS_BOTOES[i] != null)
                {
                    btnCustom[i].Text = CONFIGS_BOTOES[i].Nome;
                    btnCustom[i].BackColor = CONFIGS_BOTOES[i].Cor;
                }
            }
        }

        private async void ExecutarAcaoBotaoCustom(int index)
        {
            if (!conectadoComSucesso) return;
            var config = CONFIGS_BOTOES[index];
            if (config == null) return;

            if (!string.IsNullOrWhiteSpace(config.Comando))
            {
                string[] linhasCmd = config.Comando.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> cmdsProcessados = new List<string>();

                foreach (string linha in linhasCmd)
                {
                    string cmd = linha.Trim();
                    if (cmd.StartsWith("sudo "))
                    {
                        cmd = $"echo \"{{PASS}}\" | sudo -S {cmd.Substring(5)}";
                    }
                    cmdsProcessados.Add(cmd);
                }
                string comandoFinal = string.Join(" ; ", cmdsProcessados).Replace("{PASS}", this.PASS);
                EnviarComandoParaTerminal(config.TerminalIndex, comandoFinal);
            }

            if (config.ReiniciarAoClicar)
            {
                int termRestart = config.TerminalRestartIndex; 
                if (processos[termRestart] != null && !processos[termRestart].HasExited)
                {
                    try { processos[termRestart].Kill(); processos[termRestart].WaitForExit(); } catch { }
                }      
                await System.Threading.Tasks.Task.Delay(5000);          
                AbrirTerminal(termRestart);
            }
        }

        public void SalvarConfiguracoesBotoes()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.NomeSessaoAtual)) return;
                string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
                if (!Directory.Exists(pastaSessoes)) Directory.CreateDirectory(pastaSessoes);

                string caminhoCompleto = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

                List<string> linhas = new List<string>();
                for (int i = 0; i < LinhasArquivo.FinalBotoes + 1 - LinhasArquivo.InicioBotoes; i++)
                {
                    var c = CONFIGS_BOTOES[i] ?? new ConfigBotao();
                    string cmdSafe = c.Comando.Replace("\r\n", "[NL]").Replace("\n", "[NL]");
                    linhas.Add($"{c.Nome};{ColorTranslator.ToHtml(c.Cor)};{cmdSafe};{c.TerminalIndex};{c.ReiniciarAoClicar};{c.TerminalRestartIndex}");
                }
                for (int i = 0; i < linhas.Count; i++)
                {
                    SalvarLinhaNoArquivo(i+LinhasArquivo.InicioBotoes, linhas[i]);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao salvar botões: {ex.Message}"); }
        }

        private void CarregarConfiguracoesBotoes()
        {
            CONFIGS_BOTOES[0] = new ConfigBotao { Nome = "Botão 1", Cor = Color.Gray, Comando = "", TerminalIndex = 0, ReiniciarAoClicar = false };
            CONFIGS_BOTOES[1] = new ConfigBotao { Nome = "Botão 2", Cor = Color.Gray, Comando = "", TerminalIndex = 0, ReiniciarAoClicar = false };
            CONFIGS_BOTOES[2] = new ConfigBotao { Nome = "Botão 3", Cor = Color.Gray, Comando = "", TerminalIndex = 0, ReiniciarAoClicar = false };

            try
            {
                string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
                string caminhoCompleto = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

                if (File.Exists(caminhoCompleto))
                {
                    string[] linhas = File.ReadAllLines(caminhoCompleto);
                    for (int i = LinhasArquivo.InicioBotoes; i < Math.Min(linhas.Length, LinhasArquivo.FinalBotoes+1); i++)
                    {
                        string[] partes = linhas[i].Split(';');
                        if (partes.Length >= 5)
                        {
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes].Nome = partes[0];
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes].Cor = ColorTranslator.FromHtml(partes[1]);
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes].Comando = partes[2].Replace("[NL]", "\r\n");
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes].TerminalIndex = int.Parse(partes[3]);
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes].ReiniciarAoClicar = bool.Parse(partes[4]);
                            
                            if (partes.Length >= 6) CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes].TerminalRestartIndex = int.Parse(partes[5]);
                        }
                    }
                } else if (string.IsNullOrWhiteSpace(this.NomeSessaoAtual)) {
                    string[] linhas = this.configsSalvas.ToArray();
                    UltimoCaminhoBaixar = linhas[LinhasArquivo.UltimoDownload+1].Trim();
                    for (int i = LinhasArquivo.InicioBotoes; i < Math.Min(linhas.Length, LinhasArquivo.FinalBotoes+2); i++)
                    {
                        string[] partes = linhas[i].Split(';');
                        if (partes.Length >= 5)
                        {
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes-1].Nome = partes[0];
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes-1].Cor = ColorTranslator.FromHtml(partes[1]);
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes-1].Comando = partes[2].Replace("[NL]", "\r\n");
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes-1].TerminalIndex = int.Parse(partes[3]);
                            CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes-1].ReiniciarAoClicar = bool.Parse(partes[4]);
                            
                            if (partes.Length >= 6) CONFIGS_BOTOES[i-LinhasArquivo.InicioBotoes-1].TerminalRestartIndex = int.Parse(partes[5]);
                        }
                    }
                }
            
            } catch { }
            
        }

        private Button CriarBotao(string texto, Color cor)
        {
            Button botao = new Button {
                Text = texto,
                Width = 150,
                BackColor = cor,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                TabStop = false 
            };

            botao.Click += (s,e) => focoFantasma.Focus();

            return botao;
        }

        private void painelTopo_MouseDown(object? sender, MouseEventArgs? e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.ActiveControl = null;
                
                if (telaCheiaCustom)
                {
                    AlternarTelaCheia();
                    this.Location = new Point(Cursor.Position.X - 100, Cursor.Position.Y - 10);
                }
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private async void ReiniciarTerminaisComDelay(int delayAntesDeFechar = 2000, bool conectarAut = false)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ReiniciarTerminaisComDelay(delayAntesDeFechar, conectarAut)));
                return;
            }
            await System.Threading.Tasks.Task.Run(async () => {
                
                if (delayAntesDeFechar > 0) System.Threading.Thread.Sleep(delayAntesDeFechar);
                FecharTerminaisAtuais();           
                if (flowBotoes != null) {
                    flowBotoes.Enabled = false;
                    flowBotoes.Visible = false;
                }
                if (btnReloadTopo != null) btnReloadTopo.Enabled = false;

                bool usuarioPediuConexao = conectarAut;
                conectadoComSucesso = false;

                while (!conectadoComSucesso)
                {          
                    if (usuarioPediuConexao)
                    {
                        bool servidorPronto = ServidorEstaOnline(IP, 22);

                        if (servidorPronto)
                        {                            
                            conectadoComSucesso = true; 
                            carregarEditor = true;
                            this.Invoke(new Action(() => {
                                CarregarConfiguracoesSessao();
                            }));    
                            this.Invoke(new Action(() => { Inicializar(); }));                        
                            await System.Threading.Tasks.Task.Delay(500);
                            AtualizarVisibilidadeBotoesServidor();
                        } else {
                            await System.Threading.Tasks.Task.Delay(500);
                            conectadoComSucesso = false;
                            usuarioPediuConexao = false;
                        }
                    }

                    if (!conectadoComSucesso) using (System.Threading.ManualResetEvent waitHandle = new System.Threading.ManualResetEvent(false))
                    {
                        this.Invoke(new Action(() => {                          
                            FormTimer popup = new FormTimer();

                            popup.StartPosition = FormStartPosition.Manual;
                            popup.Location = new Point(
                                this.Location.X + (this.Width - popup.Width) / 2,
                                this.Location.Y + (this.Height - popup.Height) / 2
                            );
                            
                            popup.FormClosed += (s, ev) => {
                                usuarioPediuConexao = popup.ConexaoSolicitada;
                                
                                waitHandle.Set();
                                popup.Dispose();
                            };
                            
                            popup.Show(this); 
                        }));

                        waitHandle.WaitOne(); 
                    }                    
                }
            });
        }

        private void FecharTerminaisAtuais()
        {
            for (int i = 0; i < processos.Length; i++)
            {
                try
                {
                    if (processos[i] != null && !processos[i].HasExited)
                    {
                        processos[i].Kill(true);
                        processos[i].WaitForExit(125);
                    }
                }
                catch { }
            }
        }

        private bool VerificarSeTerminaisEstaoConectados(bool considerarTerminaisFechados = false)
        {
            if (considerarTerminaisFechados)
            {
                for (int i = 0,j = 0; i < processos.Length; i++)
                {
                    if (processos[i] == null || processos[i].HasExited)
                    {
                        j++;
                    }
                    if (j == processos.Length) return false;
                }                
                return true;
            }
            else
            {
                for (int i = 0; i < processos.Length; i++)
                {
                    if (processos[i] == null || processos[i].HasExited)
                    {
                        return false;
                    }
                }
                return true;
            }
            
        }

        private bool ServidorEstaOnline(string ip, int porta = 22)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var result = client.BeginConnect(ip, porta, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    
                    if (!success) return false;
                    
                    client.EndConnect(result);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RemoverBordaPuTTY(IntPtr handleDaJanela)
        {
            if (handleDaJanela != IntPtr.Zero)
            {
                int estiloAtual = GetWindowLong(handleDaJanela, GWL_STYLE);
                SetWindowLong(handleDaJanela, GWL_STYLE, estiloAtual & ~WS_CAPTION & ~WS_THICKFRAME);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Environment.Exit(0); 
        }

        public void FocarPeloPainel(TerminalPanel painel)
        {
            if (painel == null) return;
            this.ActiveControl = null;
            try
            {
                int index = (int)painel.Tag;
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => FocarTerminal(index)));
                }
                else
                {
                    FocarTerminal(index);
                }
            } 
            catch (Exception ex)
            {
                Debug.WriteLine("Erro ao focar painel: " + ex.Message);
            }
            
        }

        private void FocarTerminal(int index)
        {
            if (processos != null && processos[index] != null && !processos[index].HasExited)
            {
                IntPtr handle = processos[index].MainWindowHandle;
                
                BringWindowToTop(handle);
                SetActiveWindow(handle);
                SetForegroundWindow(handle);
                SetFocus(handle);
            }
        }

        private void AbrirJanelaBaixarArquivo()
        {
            Form popup = new Form
            {
                Text = "Baixar Arquivo",
                Size = new Size(450, 330),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                SizeGripStyle = SizeGripStyle.Show,
                MinimumSize = new Size(450, 330),
                MaximumSize = new Size(Screen.FromControl(this).Bounds.Width, 330),
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White
            };

            Label lblCaminho = new Label { Text = "Caminho do arquivo:", Left = 20, Top = 20, Width = 400, Font = new Font("Arial", 9, FontStyle.Bold) };
            TextBox txtCaminho = new TextBox { Left = 20, Top = 45, Width = 390, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            txtCaminho.Text = this.UltimoCaminhoBaixar;
            txtCaminho.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblNome = new Label { Text = "Nome do arquivo:", Left = 20, Top = 85, Width = 400, Font = new Font("Arial", 9, FontStyle.Bold) };
            TextBox txtNome = new TextBox { Left = 20, Top = 110, Width = 390, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtNome.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblUrl = new Label { Text = "URL de download do arquivo:", Left = 20, Top = 150, Width = 400, Font = new Font("Arial", 9, FontStyle.Bold) };
            TextBox txtUrl = new TextBox { Left = 20, Top = 175, Width = 390, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtUrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Button btnBaixar = new Button
            {
                Text = "BAIXAR",
                Left = 140,
                Top = 230,
                Width = 150,
                Height = 35,
                BackColor = Color.FromArgb(50, 200, 50),
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat
            };

            string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
            string caminhoArquivoSessao = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

            btnBaixar.Click += (sender, e) =>
            {
                string caminho = txtCaminho.Text.Trim();
                string nome = txtNome.Text.Trim();
                string url = txtUrl.Text.Trim();

                if (string.IsNullOrEmpty(caminho) || string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("Por favor, preencha todos os campos!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(this.NomeSessaoAtual) && File.Exists(caminhoArquivoSessao))
                {
                    try
                    {
                        SalvarLinhaNoArquivo(LinhasArquivo.UltimoDownload, caminho);
                        this.UltimoCaminhoBaixar = caminho;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao salvar o caminho no arquivo: {ex.Message}");
                    }
                }
                int indexPadrao = Array.IndexOf(this.COMANDOS_CUSTOMIZADOS, "");

                string comandoCurl = $"curl -L -o {caminho}/{nome} \"{url}\"";
                string comandoBaixar = Path.Combine(Path.GetTempPath(), "comando_baixar.txt");
                File.WriteAllText(comandoBaixar, comandoCurl);
                popup.Close();

                System.Threading.Tasks.Task.Run(() => {
                    System.Threading.Thread.Sleep(50); 
                    this.Invoke(new Action(() => {
                        if (indexPadrao != -1 && processos[indexPadrao] != null)EnviarComandoParaTerminal(indexPadrao, comandoCurl);
                        System.Threading.Thread.Sleep(500); 
                    }));
                });
            };

            popup.Controls.Add(lblCaminho);
            popup.Controls.Add(txtCaminho);
            popup.Controls.Add(lblNome);
            popup.Controls.Add(txtNome);
            popup.Controls.Add(lblUrl);
            popup.Controls.Add(txtUrl);
            popup.Controls.Add(btnBaixar);

            popup.ShowDialog(this);
        }

        private void CarregarConfiguracoesSessao()
        {
            CarregarConfiguracoesBotoes();
            string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
            string caminhoArquivoSessao = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

            if (!File.Exists(caminhoArquivoSessao))
            {
                while (this.configsSalvas.Count <= LinhasArquivo.UltimaLinha + 1)
                {
                    this.configsSalvas.Add(""); 
                }
                if (!string.IsNullOrWhiteSpace(this.configsSalvas[LinhasArquivo.Template + 1]))
                {

                    string[] partes = configsSalvas[LinhasArquivo.Template + 1].Split(';');
                    if (partes.Length == 3)
                    {
                        this.templateAtual = partes[0];
                        
                        string[] layoutsLidos = partes[1].Split(',');
                        for (int i = 0; i < 4; i++) layoutAtual[i] = int.Parse(layoutsLidos[i]);
                        
                        string[] dists = partes[2].Split(',');
                        for (int i = 0; i < 3; i++) distanciasSalvas[i] = int.Parse(dists[i]);

                        modoLayoutPersonalizado = true;

                        if (partes.Length >= 3)
                        {
                            string[] distancias = partes[2].Split(',');
                            for (int i = 0; i < distancias.Length && i < distanciasSalvas.Length; i++)
                            {
                                distanciasSalvas[i] = int.Parse(distancias[i]);
                            }
                        }                        
                
                        MontarEditor(this.QtdTerminais, this.templateAtual);
                    }
                }
                else
                {
                    modoLayoutPersonalizado = false;
                }
                return;
            }

            try
            {
                string[] linhas = File.ReadAllLines(caminhoArquivoSessao);
                if (!string.IsNullOrWhiteSpace(linhas[LinhasArquivo.UltimaLinha]))
                {
                    this.UltimoCaminhoBaixar = linhas[LinhasArquivo.UltimoDownload];
                }

                if (linhas.Length > LinhasArquivo.Template && !string.IsNullOrWhiteSpace(linhas[LinhasArquivo.Template]))
                {
                    string[] partes = linhas[LinhasArquivo.Template].Split(';');
                    if (partes.Length == 3)
                    {
                        this.templateAtual = partes[0];
                        
                        string[] layoutsLidos = partes[1].Split(',');
                        for (int i = 0; i < 4; i++) layoutAtual[i] = int.Parse(layoutsLidos[i]);
                        
                        string[] dists = partes[2].Split(',');
                        for (int i = 0; i < 3; i++) distanciasSalvas[i] = int.Parse(dists[i]);

                        modoLayoutPersonalizado = true;

                        if (partes.Length >= 3)
                        {
                            string[] distancias = partes[2].Split(',');
                            for (int i = 0; i < distancias.Length && i < distanciasSalvas.Length; i++)
                            {
                                distanciasSalvas[i] = int.Parse(distancias[i]);
                            }
                        }                        
                
                        MontarEditor(this.QtdTerminais, this.templateAtual);
                    }
                }
                else
                {
                    modoLayoutPersonalizado = false;
                }
            }
            catch 
            {

            }
        }

        private void SalvarLinhaNoArquivo(int indice, string novoConteudo)
        {
            string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
            string caminhoArquivoSessao = Path.Combine(pastaSessoes, $"{this.NomeSessaoAtual}_painelserverusers.txt");

            if (!File.Exists(caminhoArquivoSessao)) return;

            List<string> linhas = File.ReadAllLines(caminhoArquivoSessao).ToList();

            while (linhas.Count <= indice)
            {
                linhas.Add("");
            }

            linhas[indice] = novoConteudo;

            File.WriteAllLines(caminhoArquivoSessao, linhas);
        }

    }   

    public class TerminalPanel : Panel
    {
        [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        private const uint GW_CHILD = 5;

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_ACTIVATE = 1;
        private const int WM_PARENTNOTIFY = 0x0210;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        public TerminalPanel() 
        { 
            this.TabStop = true;
        }

        private void AvisarFormParaFocar()
        {
            if (this.IsHandleCreated && this.FindForm() is Form1 formPrincipal)
            {
                formPrincipal.BeginInvoke(new Action(() =>
                {
                    formPrincipal.FocarPeloPainel(this);
                }));
            }
        }

        private void ForcarFocoNoPutty()
        {
            IntPtr puttyHandle = GetWindow(this.Handle, GW_CHILD);
            
            if (puttyHandle != IntPtr.Zero)
            {
                SetFocus(puttyHandle);
            }
        }

        private bool MouseSobreControleWinForms()
        {
            Point posMouse = this.PointToClient(Cursor.Position);
            
            Control controle = this.GetChildAtPoint(posMouse);
            
            return controle != null;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                if (MouseSobreControleWinForms())
                {
                    base.WndProc(ref m);
                    return;
                }

                m.Result = (IntPtr)MA_ACTIVATE;
                AvisarFormParaFocar();
                ForcarFocoNoPutty();
                return; 
            }

            if (m.Msg == WM_PARENTNOTIFY)
            {
                int cliqueAcao = m.WParam.ToInt32() & 0xFFFF;
                if (cliqueAcao == WM_LBUTTONDOWN || cliqueAcao == WM_RBUTTONDOWN)
                {
                    if (!MouseSobreControleWinForms())
                    {
                        AvisarFormParaFocar();
                        ForcarFocoNoPutty();
                    }
                }
            }

            base.WndProc(ref m);
        }
    }

    public static class LinhasArquivo
    {
        public const int IP = 0;
        public const int Usuario = IP + 1;
        public const int Senha = Usuario + 1;
        public const int InicioCmdInicial = Senha + 1;
        public const int FinalCmdInicial = InicioCmdInicial + 3;
        public const int UltimoDownload = FinalCmdInicial + 1;
        public const int InicioBotoes = UltimoDownload + 1;
        public const int FinalBotoes = InicioBotoes + 2;
        public const int MAC = FinalBotoes + 1;
        public const int QtdTerminais = MAC + 1;
        public const int Template = QtdTerminais + 1;
        public const int UltimaLinha = Template;
    }

    public class FormLogin : Form
    {
        public string IP { get; private set; } = "";
        public string User { get; private set; } = "";
        public string Pass { get; private set; } = "";
        public string MacAddress { get; private set; } = "";
        public int QtdTerminais { get; private set; } = 4;
        private ComboBox cmbQtd;
        public string[] ComandosCustomizados { get; private set; } = new string[4] { "", "", "", "" };
        public string SessaoSelecionada { get; private set; } = "";
        public List<string> ConfigsLidas { get; private set; } = new List<string>();

        private ComboBox cmbSessao;
        private TextBox txtIP;
        private TextBox txtUser;
        private TextBox txtPass;
        private TextBox txtMac;
        private CheckBox chkSalvar;
        private Button btnAvancado;
        private Panel pnlAvancado;
        private Button btnConectar;
        private TextBox[] txtComandos = new TextBox[4];
        private bool abertao = false;

        private string ObterCaminhoPastaSessoes()
        {
            string pastaSessoes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessoessalvas");
            if (!Directory.Exists(pastaSessoes)) Directory.CreateDirectory(pastaSessoes);
            return pastaSessoes;
        }

        public FormLogin()
        {
            this.Icon = new System.Drawing.Icon("icone.ico");
            this.Text = "Conexão SSH - Painel Servidor";
            this.Size = new Size(380, 340);
            this.FormBorderStyle = FormBorderStyle.Sizable; 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(380, 340);
            this.MaximumSize = new Size(Screen.FromControl(this).Bounds.Width, 340);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            Label lblSessao = new Label() { Text = "Nome da Sessão:", Location = new Point(20, 22), Size = new Size(100, 20) };
            cmbSessao = new ComboBox() { Location = new Point(130, 20), Size = new Size(190, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDown };
            cmbSessao.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbSessao.SelectedIndexChanged += CmbSessao_SelectedIndexChanged;

            Label lblIP = new Label() { Text = "Endereço IP:", Location = new Point(20, 57), Size = new Size(100, 20) };
            txtIP = new TextBox() { Location = new Point(130, 55), Size = new Size(190, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            txtIP.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblUser = new Label() { Text = "Usuário SSH:", Location = new Point(20, 92), Size = new Size(100, 20) };
            txtUser = new TextBox() { Location = new Point(130, 90), Size = new Size(190, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            txtUser.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            
            Label lblPass = new Label() { Text = "Senha SSH:", Location = new Point(20, 127), Size = new Size(100, 20) };
            txtPass = new TextBox() { Location = new Point(130, 125), Size = new Size(190, 20), PasswordChar = '*', BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            txtPass.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblMac = new Label() { Text = "MAC (Opcional):", Location = new Point(20, 162), Size = new Size(100, 20) };
            txtMac = new TextBox() { Location = new Point(130, 160), Size = new Size(190, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            txtMac.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblQtd = new Label() { Text = "Terminais:", Location = new Point(20, 197), Size = new Size(100, 20) };
            cmbQtd = new ComboBox() { Location = new Point(130, 195), Size = new Size(190, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbQtd.Items.AddRange(new object[] { "1", "2", "3", "4" });
            cmbQtd.SelectedIndex = 3;
            cmbQtd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            chkSalvar = new CheckBox() { Text = "Salvar sessão", Location = new Point(130, 230), Size = new Size(190, 20) };
            chkSalvar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnAvancado = new Button() { Text = "Configurações Avançadas ⚙", Size = new Size(170, 30), FlatStyle = FlatStyle.Flat };
            btnAvancado.FlatAppearance.BorderColor = Color.Gray;
            btnAvancado.Click += BtnAvancado_Click;

            btnConectar = new Button() { Text = "CONECTAR", Size = new Size(110, 30), FlatStyle = FlatStyle.Flat, Font = new Font(this.Font, FontStyle.Bold) };
            btnConectar.FlatAppearance.BorderColor = Color.LimeGreen;
            btnConectar.ForeColor = Color.LimeGreen;
            btnConectar.Click += BtnConectar_Click;

            pnlAvancado = new Panel() { Visible = false };
            
            Label lblExplica = new Label() { Text = "Comandos iniciais (web: ':[porta]'):", Location = new Point(0, 0), Size = new Size(320, 18), ForeColor = Color.DarkGray, Font = new Font(this.Font, FontStyle.Italic), TextAlign = ContentAlignment.MiddleLeft };
            lblExplica.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlAvancado.Controls.Add(lblExplica);

            for (int i = 0; i < 4; i++)
            {
                Label lblTerm = new Label() { Text = $"Term {i + 1}:", Location = new Point(0, 25 + (i * 28)), Size = new Size(50, 20) };
                txtComandos[i] = new TextBox() { Location = new Point(55, 23 + (i * 28)), Size = new Size(120, 20), BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.LimeGreen };
                txtComandos[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                pnlAvancado.Controls.AddRange(new Control[] { lblTerm, txtComandos[i] });                
            }

            this.Controls.AddRange(new Control[] { lblSessao, cmbSessao, lblIP, txtIP, lblUser, txtUser, lblPass, txtPass, lblMac, txtMac, lblQtd, cmbQtd, chkSalvar, btnAvancado, btnConectar, pnlAvancado });

            this.Resize += FormLogin_Resize;
            this.Load += FormLogin_Load;

            CarregarSessoesSalvas();
        }

        private void FormLogin_Load(object? sender, EventArgs e)
        {
            AjustarPosicoesDinamicas();
        }

        private void BtnAvancado_Click(object? sender, EventArgs e)
        {
            if (!abertao)
            {
                abertao = true;
                this.MinimumSize = new Size(380, 500);
                this.MaximumSize = new Size(Screen.FromControl(this).Bounds.Width, 500);
                this.Size = new Size(this.Size.Width, 500);                
                pnlAvancado.Visible = true;
                btnAvancado.Text = "Ocultar Avançadas ✖";
            }
            else
            {
                abertao = false;
                pnlAvancado.Visible = false;
                this.MinimumSize = new Size(380, 340);
                this.MaximumSize = new Size(Screen.FromControl(this).Bounds.Width, 340);
                this.Size = new Size(this.Size.Width, 340);
                btnAvancado.Text = "Configurações Avançadas ⚙";
            }
            
            AjustarPosicoesDinamicas();
        }

        private void CarregarSessoesSalvas()
        {
            try
            {
                string pastaSessoes = ObterCaminhoPastaSessoes();
                string[] arquivos = Directory.GetFiles(pastaSessoes, "*_painelserverusers.txt");
                cmbSessao.Items.Clear();
                foreach (string arquivo in arquivos)
                {
                    cmbSessao.Items.Add(Path.GetFileName(arquivo).Replace("_painelserverusers.txt", ""));
                }
            }
            catch {}
        }

        private void CmbSessao_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbSessao.SelectedItem == null) return;
            string caminhoArquivo = Path.Combine(ObterCaminhoPastaSessoes(), $"{cmbSessao.SelectedItem}_painelserverusers.txt");

            if (File.Exists(caminhoArquivo))
            {
                try
                {
                    string[] linhas = File.ReadAllLines(caminhoArquivo);
                    if (linhas.Length >= 3)
                    {
                        txtIP.Text = linhas[LinhasArquivo.IP];
                        txtUser.Text = linhas[LinhasArquivo.Usuario];
                        txtPass.Text = linhas[LinhasArquivo.Senha];
                        chkSalvar.Checked = true;
                        
                        for (int i = LinhasArquivo.FinalCmdInicial; i >= LinhasArquivo.InicioCmdInicial; i--) txtComandos[LinhasArquivo.FinalCmdInicial - i].Text = "";

                        for (int i = LinhasArquivo.FinalCmdInicial; i >= LinhasArquivo.InicioCmdInicial; i--)
                        {
                            if (linhas.Length > i - LinhasArquivo.InicioCmdInicial)
                            {
                                txtComandos[LinhasArquivo.FinalCmdInicial-i].Text = linhas[LinhasArquivo.FinalCmdInicial-i+LinhasArquivo.InicioCmdInicial];
                            }
                        }

                        if (linhas.Length > LinhasArquivo.MAC) txtMac.Text = linhas[LinhasArquivo.MAC];
                        if (linhas.Length > LinhasArquivo.QtdTerminais) cmbQtd.SelectedItem = linhas[LinhasArquivo.QtdTerminais];
                        else txtMac.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar dados: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnConectar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cmbSessao.Text) || string.IsNullOrWhiteSpace(txtIP.Text) || string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text)){
                MessageBox.Show("Por favor, preencha as credenciais de acesso.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.IP = txtIP.Text.Trim();
            this.User = txtUser.Text.Trim();
            this.Pass = txtPass.Text;
            this.MacAddress = txtMac.Text;
            this.SessaoSelecionada = cmbSessao.Text.Trim();
            this.QtdTerminais = int.Parse(cmbQtd.Text);
            
            for (int i = LinhasArquivo.FinalCmdInicial; i >= LinhasArquivo.InicioCmdInicial; i--)
            {
                this.ComandosCustomizados[LinhasArquivo.FinalCmdInicial-i] = txtComandos[LinhasArquivo.FinalCmdInicial-i].Text.Trim();
            }

            if (chkSalvar.Checked)
            {
                this.SessaoSelecionada = cmbSessao.Text.Trim();
                try
                {
                    string caminhoCompleto = Path.Combine(ObterCaminhoPastaSessoes(), $"{cmbSessao.Text.Trim()}_painelserverusers.txt");
                    List<string> linhasArquivo = File.ReadAllLines(caminhoCompleto).ToList();
                    
                    List<string> dadosParaSalvar = new List<string> { this.IP, this.User, this.Pass };
                    for (int i = LinhasArquivo.FinalCmdInicial; i >= LinhasArquivo.InicioCmdInicial; i--)
                    {
                        dadosParaSalvar.Add(this.ComandosCustomizados[LinhasArquivo.FinalCmdInicial-i]);
                    }

                    for (int i = LinhasArquivo.UltimoDownload; i < LinhasArquivo.FinalBotoes+1; i++)
                    {
                        dadosParaSalvar.Add(linhasArquivo[i]);
                    }
                    dadosParaSalvar.Add(this.MacAddress);
                    dadosParaSalvar.Add(this.QtdTerminais.ToString());
                    dadosParaSalvar.Add(linhasArquivo.Count > LinhasArquivo.Template && this.QtdTerminais.ToString() == linhasArquivo[LinhasArquivo.QtdTerminais] ? linhasArquivo[LinhasArquivo.Template] : "");
                    File.WriteAllLines(caminhoCompleto, dadosParaSalvar);
                }
                catch 
                {
                    string caminhoCompleto = Path.Combine(ObterCaminhoPastaSessoes(), $"{cmbSessao.Text.Trim()}_painelserverusers.txt");
                    File.WriteAllLines(caminhoCompleto, new string[] { this.IP, this.User, this.Pass });
                    List<string> dadosParaSalvar = new List<string> { this.IP, this.User, this.Pass };
                    for (int i = LinhasArquivo.FinalCmdInicial; i >= LinhasArquivo.InicioCmdInicial; i--)
                    {
                        dadosParaSalvar.Add(this.ComandosCustomizados[LinhasArquivo.FinalCmdInicial-i]);
                    }                    

                    for (int i = LinhasArquivo.UltimoDownload; i < LinhasArquivo.FinalBotoes+1; i++)
                    {
                        dadosParaSalvar.Add("");
                    }

                    dadosParaSalvar.Add(this.MacAddress);
                    dadosParaSalvar.Add(this.QtdTerminais.ToString());
                    dadosParaSalvar.Add("");
                    File.WriteAllLines(caminhoCompleto, dadosParaSalvar);
                }
            } else {
                try{
                    ConfigsLidas.Add(this.SessaoSelecionada);
                    ConfigsLidas.AddRange(File.ReadAllLines(Path.Combine(ObterCaminhoPastaSessoes(), $"{cmbSessao.Text.Trim()}_painelserverusers.txt")).ToList());
                }
                catch 
                {
                    ConfigsLidas = new List<string>();
                }
                this.SessaoSelecionada = "";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FormLogin_Resize(object? sender, EventArgs e)
        {
            AjustarPosicoesDinamicas();
        }

        private void AjustarPosicoesDinamicas()
        {
            int larguraDisponivel = this.ClientSize.Width;
            int alturaDisponivel = this.ClientSize.Height;

            if (btnConectar != null)
            {
                btnConectar.Location = new Point(larguraDisponivel - btnConectar.Width - 20, 260);
            }

            if (!abertao)
            {
                btnAvancado.Location = new Point(20, 260);
            }
            else
            {
                btnAvancado.Location = new Point(20, 260);
                pnlAvancado.Location = new Point(20, 305);
                pnlAvancado.Width = larguraDisponivel - 40;
                pnlAvancado.Height = alturaDisponivel - 275;
            }
        }
    }

    public class FormPopupComandos : Form
    {
        public string[] ComandosRespondidos { get; private set; } = new string[4];
        public string MacRespondido { get; private set; } = "";
        public int QtdTerminaisRespondido { get; private set; } = 4;
        private TextBox[] txtComandosPopup = new TextBox[4];
        private TextBox txtMacPopup;
        private ComboBox cmbQtdPopup;
        private Button btnSalvarPopup = new Button();

        public FormPopupComandos(string nomeSessao, string[] comandosAtuais, string macAtual, int qtdAtual)
        {
            this.Icon = new System.Drawing.Icon("icone.ico");
            this.Text = "Configurações Avançadas de Comandos";
            this.Size = new Size(400, 310);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.SizeGripStyle = SizeGripStyle.Show;
            this.MinimumSize = new Size(400, 310);
            this.MaximumSize = new Size(Screen.FromControl(this).Bounds.Width, 310);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(35, 35, 35);
            this.ForeColor = Color.White;

            Label lblNomeSessao = new Label() { Text = $"{nomeSessao}", Location = new Point(15, 10), Anchor = AnchorStyles.Top | AnchorStyles.Left |  AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(350, 18), ForeColor = Color.LimeGreen, Font = new Font(this.Font, FontStyle.Bold) };
            this.Controls.Add(lblNomeSessao);

            Label lblExplica = new Label() { Text = "Comandos iniciais (web: ':[porta]'):", Location = new Point(15, 30), Size = new Size(320, 18), ForeColor = Color.DarkGray, Font = new Font(this.Font, FontStyle.Italic), TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblExplica);

            for (int i = 0; i < 4; i++)
            {
                Label lblTerm = new Label() { Text = $"Term {i + 1}:", Location = new Point(15, 57 + (i * 28)), Size = new Size(50, 20) };
                txtComandosPopup[i] = new TextBox() { Location = new Point(70, 55 + (i * 28)), Size = new Size(270, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.LimeGreen };
                txtComandosPopup[i].Text = comandosAtuais[i];
                txtComandosPopup[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                Button btnRestart = new Button() { Text = "↻", Location = new Point(347, 55 + (i * 28)), Size = new Size(23, 23), FlatStyle = FlatStyle.Flat };
                btnRestart.BackColor = Color.FromArgb(60, 65, 60);
                btnRestart.Font = new Font("Arial", 9, FontStyle.Bold);
                btnRestart.ForeColor = Color.White;
                btnRestart.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                int indexTerminal = i;
                btnRestart.Click += (s, e) => 
                {
                    Form1 formPrincipal = System.Windows.Forms.Application.OpenForms.OfType<Form1>().FirstOrDefault();
                    if (formPrincipal != null)
                    {
                        formPrincipal.ExecutarReiniciarTerminal(indexTerminal);
                        btnSalvarPopup.PerformClick();
                    }
                };
                
                this.Controls.AddRange(new Control[] { lblTerm, txtComandosPopup[i], btnRestart });
            }

            Label lblMac = new Label() { Text = "MAC PC:", Location = new Point(15, 172), Size = new Size(60, 20) };
            txtMacPopup = new TextBox() { Text = macAtual, Location = new Point(80, 170), Size = new Size(290, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            txtMacPopup.Text = macAtual;
            txtMacPopup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.AddRange(new Control[] { lblMac, txtMacPopup });

            Label lblQtd = new Label() { Text = "Terminais:", Location = new Point(15, 202), Size = new Size(60, 20) };
            cmbQtdPopup = new ComboBox() { Location = new Point(80, 200), Size = new Size(70, 20), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbQtdPopup.Items.AddRange(new object[] { "1", "2", "3", "4" });
            cmbQtdPopup.SelectedIndex = qtdAtual - 1; 
            this.Controls.AddRange(new Control[] { lblQtd, cmbQtdPopup });

            Button btnLayoutPers = new Button() { Text = "Editar Layout ◫", Location = new Point(170, 200), Size = new Size(130, 24), FlatStyle = FlatStyle.Flat };
            btnLayoutPers.BackColor = Color.FromArgb(70, 100, 90);
            btnLayoutPers.ForeColor = Color.White;
            btnLayoutPers.Click += (s, e) => {
                this.QtdTerminaisRespondido = int.Parse(cmbQtdPopup.Text);
                this.DialogResult = DialogResult.Retry; 
                this.Close();
            };
            this.Controls.Add(btnLayoutPers);

            Button btnEditarBotoes = new Button(){ Text = "EDITAR BOTÕES", Location = new Point(70, 230), Size = new Size(100, 25), FlatStyle = FlatStyle.Flat };
            btnEditarBotoes.BackColor = Color.FromArgb(60, 80, 60);
            btnEditarBotoes.ForeColor = Color.White;
            btnEditarBotoes.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnEditarBotoes.Click += (s, e) => {
                Form1 formPrincipal = System.Windows.Forms.Application.OpenForms.OfType<Form1>().FirstOrDefault();
                
                if (formPrincipal != null)
                {
                    using (FormEditorBotoes editor = new FormEditorBotoes(formPrincipal.CONFIGS_BOTOES))
                    {
                        if (editor.ShowDialog() == DialogResult.OK)
                        {
                            formPrincipal.CONFIGS_BOTOES = editor.NovosDados;
                            formPrincipal.SalvarConfiguracoesBotoes();
                            formPrincipal.Invoke(new Action(() => {
                                formPrincipal.RecarregarVisualEBotoes();
                            }));
                        }
                    }
                }
                else
                {
                    MessageBox.Show("A janela principal (Form1) não foi encontrada na memória.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnEditarBotoes);

            Button btnLigarPc = new Button(){ Text = "LIGAR PC", Location = new Point(195, 230), Size = new Size(75, 25), FlatStyle = FlatStyle.Flat };
            btnLigarPc.BackColor = Color.FromArgb(50, 100, 200);
            btnLigarPc.ForeColor = Color.White;
            btnLigarPc.Click += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtMacPopup.Text)) {
                    try {
                        Form1.EnviarWakeOnLan(txtMacPopup.Text.Trim());
                        MessageBox.Show("Sinal de Wake-on-LAN enviado com sucesso!", "Ligando...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    } 
                    catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
                } else {
                    MessageBox.Show("Preencha o MAC Address primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            this.Controls.Add(btnLigarPc);

            btnSalvarPopup = new Button() { Text = "SALVAR", Location = new Point(295, 230), Size = new Size(75, 25), FlatStyle = FlatStyle.Flat };
            btnSalvarPopup.FlatAppearance.BorderColor = Color.LimeGreen;
            btnSalvarPopup.ForeColor = Color.LimeGreen;
            btnSalvarPopup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSalvarPopup.Click += (s, e) => {
                for (int i = 0; i < 4; i++)
                {
                    this.ComandosRespondidos[i] = txtComandosPopup[i].Text.Trim();
                }
                this.MacRespondido = txtMacPopup.Text;
                this.QtdTerminaisRespondido = int.Parse(cmbQtdPopup.Text);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(btnSalvarPopup);
        }
    }

    public class ConfigBotao
    {
        public string Nome { get; set; } = "";
        public Color Cor { get; set; } = Color.Gray;
        public string Comando { get; set; } = "";
        public int TerminalIndex { get; set; } = 0;
        public bool ReiniciarAoClicar { get; set; } = false;
        public int TerminalRestartIndex { get; set; } = 0;
    }

    public class FormEditorBotoes : Form
    {
        public ConfigBotao[] NovosDados { get; private set; } = new ConfigBotao[3];

        private TextBox[] txtNomes = new TextBox[3];
        private Button[] btnCores = new Button[3];
        private TextBox[] txtComandos = new TextBox[3];
        private ComboBox[] cmbTerminais = new ComboBox[3];
        private CheckBox[] chkReiniciar = new CheckBox[3];
        private ComboBox[] cmbTerminaisRestart = new ComboBox[3];

        public FormEditorBotoes(ConfigBotao[] dadosAtuais)
        {
            this.Icon = new System.Drawing.Icon("icone.ico");
            this.Text = "Configurar Botões Superiores";
            this.Size = new Size(550, 600);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.SizeGripStyle = SizeGripStyle.Show;
            this.MinimumSize = new Size(550, 600);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(35, 35, 35);
            this.ForeColor = Color.White;

            TableLayoutPanel layoutMain = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            layoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            layoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            layoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            layoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            for (int i = 0; i < 3; i++)
            {
                NovosDados[i] = new ConfigBotao
                {
                    Nome = dadosAtuais[i].Nome, Cor = dadosAtuais[i].Cor, Comando = dadosAtuais[i].Comando,
                    TerminalIndex = dadosAtuais[i].TerminalIndex, ReiniciarAoClicar = dadosAtuais[i].ReiniciarAoClicar,
                    TerminalRestartIndex = dadosAtuais[i].TerminalRestartIndex
                };

                GroupBox gbox = new GroupBox { Text = $"Configuração do Botão {i + 1}", Dock = DockStyle.Fill, ForeColor = Color.LimeGreen, Margin = new Padding(10), Size = new Size(500, 160) };
                
                Label lblNome = new Label { Text = "Nome:", Location = new Point(15, 25), Size = new Size(50, 20), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left };
                txtNomes[i] = new TextBox { Text = NovosDados[i].Nome, Location = new Point(70, 22), Size = new Size(150, 20), BackColor = Color.FromArgb(50,50,50), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left };

                Label lblCor = new Label { Text = "Cor:", Location = new Point(240, 25), Size = new Size(35, 20), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left };
                btnCores[i] = new Button { BackColor = NovosDados[i].Cor, Location = new Point(280, 21), Size = new Size(40, 23), FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Top | AnchorStyles.Left };
                int indexCor = i;
                btnCores[i].Click += (s, e) => {
                    using (ColorDialog cd = new ColorDialog { Color = btnCores[indexCor].BackColor })
                    {
                        if (cd.ShowDialog() == DialogResult.OK)
                        {
                            btnCores[indexCor].BackColor = cd.Color;
                            NovosDados[indexCor].Cor = cd.Color;
                        }
                    }
                };

                Label lblTerminal = new Label { Text = "Enviar p/:", Location = new Point(315, 25), Size = new Size(80, 20), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                cmbTerminais[i] = new ComboBox { Location = new Point(400, 21), Size = new Size(80, 23), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                cmbTerminais[i].Items.AddRange(new object[] { "Terminal 1", "Terminal 2", "Terminal 3", "Terminal 4" });
                cmbTerminais[i].SelectedIndex = NovosDados[i].TerminalIndex;

                Label lblComando = new Label { Text = "Comandos:\n1 por linha, não use '?'\nweb: 'acessar :[porta]'", Location = new Point(15, 52), Size = new Size(135, 52), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter};
                
                txtComandos[i] = new TextBox { Text = NovosDados[i].Comando, Location = new Point(150, 52), Size = new Size(330, 60), Multiline = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

                chkReiniciar[i] = new CheckBox { Text = "Reiniciar também o:", Checked = NovosDados[i].ReiniciarAoClicar, Location = new Point(15, 120), Size = new Size(130, 20), ForeColor = Color.White, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
                cmbTerminaisRestart[i] = new ComboBox { Location = new Point(150, 118), Size = new Size(80, 23), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
                cmbTerminaisRestart[i].Items.AddRange(new object[] { "Terminal 1", "Terminal 2", "Terminal 3", "Terminal 4" });
                cmbTerminaisRestart[i].SelectedIndex = NovosDados[i].TerminalRestartIndex;

                gbox.Controls.AddRange(new Control[] { lblNome, txtNomes[i], lblCor, btnCores[i], lblTerminal, cmbTerminais[i], lblComando, txtComandos[i], chkReiniciar[i], cmbTerminaisRestart[i] });
                layoutMain.Controls.Add(gbox, 0, i);
            }

            Button btnSalvar = new Button { Text = "SALVAR ALTERAÇÕES DOS BOTÕES", Size = new Size(300, 40), Anchor = AnchorStyles.None, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 120, 45), ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnSalvar.Click += (s, e) => {
                for (int i = 0; i < 3; i++)
                {
                    NovosDados[i].Nome = txtNomes[i].Text.Trim();
                    NovosDados[i].Comando = txtComandos[i].Text.Trim();
                    NovosDados[i].TerminalIndex = cmbTerminais[i].SelectedIndex;
                    NovosDados[i].ReiniciarAoClicar = chkReiniciar[i].Checked;
                    NovosDados[i].TerminalRestartIndex = cmbTerminaisRestart[i].SelectedIndex;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            layoutMain.Controls.Add(btnSalvar, 0, 3);
            this.Controls.Add(layoutMain);
        }
    }

    public class FormEscolhaTemplate : Form
    {
        public string TemplateEscolhido { get; private set; } = "";

        public FormEscolhaTemplate(int qtdTerminais)
        {
            this.Icon = new System.Drawing.Icon("icone.ico");
            this.Text = "Escolha o Layout";
            this.Size = new Size(350, 120);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.BackColor = Color.FromArgb(35, 35, 35);

            FlowLayoutPanel flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            this.Controls.Add(flow);

            if (qtdTerminais == 2)
            {
                flow.Controls.Add(CriarBotao("Lado a Lado (Vertical)", "2V"));
                flow.Controls.Add(CriarBotao("Topo e Base (Horizontal)", "2H"));
            }
            else if (qtdTerminais == 3)
            {
                this.Size = new Size(350, 180);
                flow.Controls.Add(CriarBotao("1 Esquerda, 2 Direita", "3E"));
                flow.Controls.Add(CriarBotao("2 Esquerda, 1 Direita", "3D"));
                flow.Controls.Add(CriarBotao("1 Topo, 2 Base", "3T"));
                flow.Controls.Add(CriarBotao("2 Topo, 1 Base", "3B"));
            }
        }

        private Button CriarBotao(string texto, string codigo)
        {
            Button btn = new Button { Text = texto, Size = new Size(150, 40), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => { TemplateEscolhido = codigo; this.DialogResult = DialogResult.OK; this.Close(); };
            return btn;
        }
    }
}