using Cryptool.LFSR;
using Cryptool.PluginBase.Utils;
using Cryptool.PluginBase.Utils.Logging;
using Cryptool.PluginBase.Utils.ObjectDeconstruct;
using Cryptool.PluginBase.Utils.StandaloneComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptool.PluginBase.Utils.Datatypes;
using static Cryptool.PluginBase.Utils.Datatypes.Datatypes;
using Cryptool.PluginBase.Utils.StandaloneComponent.Program;

// This is for debugging: Add an expression watch with "D.To.Str(<your-variable>)" and see it pretty-printed.
namespace D
{
    // "current" object for debugging
    public static class Current
    {
        public static LFSRRecord R;
        public static dynamic f1; // some field
    }
    public static class To
    {
        public static String Str(object o) => Cryptool.LFSR.ConvertTo.String(o);
    }
}

namespace Cryptool.LFSR
{

    public class LFSRStandaloneComponent : AbstractStandaloneComponent<LFSRAPI, LFSRParameters>
    {
        public LFSRStandaloneComponent(LFSRParameters parameters) : base(new LFSRAPI(parameters)) { }
        public override System.Windows.Controls.UserControl Presentation => throw new NotImplementedException();
    }

    public class LFSRRecord : ComponentSyncProgramRecord<LFSRAPI, LFSRParameters>
    {
        public LFSRRecord(
            Func<IStandaloneComponent<LFSRAPI, LFSRParameters>> programSpec,
            IComponentProgramInteraction<LFSRAPI, LFSRParameters> interaction,
            String name = "Generic"
            ) : base(programSpec, interaction)
        {

            Name = name;
        }

        public string Name { get; }

        public HistoryBox<bool> OutBit = new HistoryBox<bool>();
        public HistoryBox<bool[]> OutBits = new HistoryBox<bool[]>();
        public HistoryBox<string> OutStatesString = new HistoryBox<String>();
        public HistoryBox<string> OutString = new HistoryBox<String>();
        public HistoryBox<List<bool>> LFSRRegHistory = new HistoryBox<List<bool>>();


        public override void ComponentCreated()
        {
            API.OutputAsStatesString.OnChange += OutStatesString.Record;
            API.OutputAsBits.OnChange += OutBits.Record;
            API.OutputAsBit.OnChange += OutBit.Record;
            API.OutputAsString.OnChange += OutString.Record;
            API.OnRoundFinished += () =>
            {
                LFSRRegHistory.Record(API.currentRound.RegInitial.Bits);
            };
        }

        internal void LogResults()
        {

        }
    }

    public static class LFSRTests
    {
        public static void Assert(bool value, String logMsg = null)
        {
            if (!value) throw new Exception(logMsg ?? "Assert failed");
        }

        public static LFSRStandaloneComponent LFSRConfig10RoundsNoCLK()
        {
            var param = new LFSRParameters();
            param.UseClock.Value = false;
            param.DisablePresentation.Value = true;
            param.Rounds.Value = 10;
            return new LFSRStandaloneComponent(param);
        }

        public static LFSRStandaloneComponent LFSRConfiguration5RoundsCLK()
        {
            var param = new LFSRParameters();
            param.UseClock.Value = true;
            param.DisablePresentation.Value = true;
            param.Rounds.Value = 5;
            return new LFSRStandaloneComponent(param);
        }

        // Interaction 1: 4-bit Fibonacci LFSR from Anne Canteaut [6], section 3.1, page 44:

        // c = tap = 0011
        // seed = 1011
        // Expected output starts with:         1011 1100 0100 1101 = 0xBC4D
        // Expected output(after first stage):  1100 0100 1101
        public static IComponentProgramInteraction<LFSRAPI, LFSRParameters> InteractionAnneCanteaut44()
        {
            return SyncInter.CompleteExecAfterEach<LFSRAPI, LFSRParameters>(Sequence<Action<LFSRAPI>>(
                api => api.InputPoly.Value = "0011",
                api => api.InputSeed.Value = "1011"
            ));
        }

        // Interaction 2: 16 - bit Fibonacci LFSR from Wikipedia[1], see Figure 1 above:

        // c = tap = 0000 0000 0010 1101
        // seed = 1010 1100 1110 0001 = 0xACE1
        // Expected output(after first stage):
        // 0101 0110 0111 0000 = 0x5670
        public static IComponentProgramInteraction<LFSRAPI, LFSRParameters> InteractionWikipedia()
        {
            return SyncInter.CompleteExecAfterEach<LFSRAPI, LFSRParameters>(Sequence<Action<LFSRAPI>>(
                api => api.InputPoly.Value = "1011 1100 0100 1101",
                api => api.InputSeed.Value = "1010 1100 1110 0001 ",
                api => api.InputClock.Value = false,
                api => api.InputClock.Value = true,

                api => api.InputClock.Value = false,
                api => api.InputClock.Value = true,

                api => api.InputClock.Value = false,
                api => api.InputClock.Value = true,

                api => api.InputClock.Value = false,
                api => api.InputClock.Value = true,

                api => api.InputClock.Value = false,
                api => api.InputClock.Value = true
            ));
        }

        // -------- Combine (parameter) configuration and (input sequence) interactions into test cases

        public static LFSRRecord case1Record = new LFSRRecord(
            LFSRConfig10RoundsNoCLK,
            InteractionAnneCanteaut44(),
            "Canteaut10RoundsNoCLK"
            );

        public static LFSRRecord case2Record = new LFSRRecord(
            LFSRConfiguration5RoundsCLK,
            InteractionWikipedia(),
            "Wikipedia5RoundsWithCLK"
            );

        public static void LogThis(this object msg, Logger logger=null, LogLevel lvl=null)
        {
            var level = lvl ?? LogLevels.Debug;
            var log = logger ?? GlobalLog.VSDebug;
            LFSR.ConvertTo.Log(msg, level, log);
        }
        public static void Main(string[] args)
        {
            Logger logger = new Logger("LFSR Tests");
            GlobalLog.VSDebug.Receiving(logger);
            GlobalLog.CErr.Receiving(logger);

            case1Record.Run();

//             case2Record.OnComponentCreated += c => c.api.OnRoundStarting += () => LogThis("case 2 round triggered");
            case2Record.Run();

            logger.prefix = "LFSR Tests, Case 1";

            "-------------  Single bit history:".LogThis(logger);
            case1Record.OutBit.History.LogThis(logger);
            
            "-------------  All output bits history:".LogThis(logger);
            case1Record.OutBits.History.LogThis(logger);

            "-------------  All register states:".LogThis(logger);
            case1Record.OutStatesString.Last.LogThis(logger);

            "-------------  Output string history:".LogThis(logger);
            case1Record.OutString.History.LogThis(logger);

            // Assert the correctness of the output...
            Assert(case1Record.OutString.Last.Equals("1011110001"));
            
            logger.prefix = "LFSR Tests, Case 2";
            case2Record.OutBit.History.LogThis(logger);
            case2Record.ProgressHistory.History.LogThis(logger);
        }

    }
}

