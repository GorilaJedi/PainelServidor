using System;
using System.Drawing;
using System.Windows.Forms;

public class FormTimer : Form
{
    private Label lblMensagem;
    private Button btnConectarAgora;
    private System.Windows.Forms.Timer timerContagem;
    private int tempoRestante = 15;
    
    public bool ConexaoSolicitada { get; private set; } = false;

    public FormTimer()
    {
        this.Text = "Aguardando Inicialização";
        this.Size = new Size(300, 130);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.None;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 30);

        lblMensagem = new Label
        {
            Text = $"⚠️ Servidor reiniciando...\nReconectando em {tempoRestante} segundos.",
            ForeColor = Color.White,
            Font = new Font("Arial", 12, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 80
        };

        btnConectarAgora = new Button
        {
            Text = "🔌 Conectar Agora",
            Size = new Size(150, 40),
            Location = new Point(75, 75),
            Font = new Font("Arial", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 150),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnConectarAgora.FlatAppearance.BorderSize = 0;
        
        btnConectarAgora.Click += (s, e) => {
            ConexaoSolicitada = true;
            this.Close();
        };

        this.Controls.Add(lblMensagem);
        this.Controls.Add(btnConectarAgora);

        timerContagem = new System.Windows.Forms.Timer { Interval = 1000 };
        timerContagem.Tick += (s, e) => {
            tempoRestante--;
            if (tempoRestante > 0)
            {
                lblMensagem.Text = $"⚠️ Servidor reiniciando...\nReconectando em {tempoRestante} segundos.";
            }
            else
            {
                timerContagem.Stop();
                ConexaoSolicitada = true;
                this.Close();
            }
        };
        
        this.Load += (s, e) => timerContagem.Start();
    }
}