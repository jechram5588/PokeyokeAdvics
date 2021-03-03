using PokayokeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PokayokeStations
{
    public partial class PokayokeStations : ServiceBase
    {
        private System.Timers.Timer timer1 = null;
        public static Pokayoke Poka = new Pokayoke();

        public PokayokeStations()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer1 = new System.Timers.Timer();
            this.timer1.Interval = 2500;
            this.timer1.Elapsed += new ElapsedEventHandler(this.timer1_Tick);
            timer1.Enabled = true;
            Poka.PrimaryLog("Inicio de Servicio", "Se ha mandado iniciar el servicio", EventLogEntryType.Warning, true);
            Poka.Running = true;
            Poka.Start();
        }
        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            if (!Poka.Running)
                this.Stop();
        }

        protected override void OnStop()
        {
            Poka.Stop();
            timer1.Stop();
            Poka.PrimaryLog("Detención del Servicio", "Se ha mandado a detener el servicio", EventLogEntryType.Warning, true);

        }
    }
}
