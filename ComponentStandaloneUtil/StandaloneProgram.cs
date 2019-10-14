using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Cryptool.PluginBase.Utils.Datatypes;
using static Cryptool.PluginBase.Utils.Datatypes.Datatypes;
using Cryptool.PluginBase.Utils.Logging;
using Cryptool.PluginBase.Utils.StandaloneComponent.Common;

namespace Cryptool.PluginBase.Utils.StandaloneComponent.Program
{

    public interface IComponentProgramInteraction<APIType, ParamType>
        where APIType : IComponentAPI<ParamType>
        where ParamType : IParameters

    {
        void Interact(IStandaloneComponent<APIType, ParamType> component, double terminateAfter);
    }
    public class SyncInteractionQueue<APIType, ParamType> : IComponentProgramInteraction<APIType, ParamType>
        where APIType : IComponentAPI<ParamType>
        where ParamType : IParameters
    {
        public IEnumerable<Action<APIType>> Events { get; }

        public SyncInteractionQueue(List<Action<APIType>> events)
        {
            this.Events = events;
        }

        public void Interact(IStandaloneComponent<APIType, ParamType> component, double terminateAfter)
        {
            foreach (var instr in Events)
            {
                instr.Invoke(component.api);
            }
        }
    }
    public static class SyncInter
    {
        public static List<Action<APIType>> LifecycleStart<APIType, ParamType>()
            where APIType : IComponentAPI<ParamType>
            where ParamType : IParameters
        {
            return Sequence<Action<APIType>>(
                api => api._raiseInitialize(),
                api => api._raisePreExecution()
            );
        }

        public static List<Action<APIType>> LifecycleEnd<APIType, ParamType>()
            where APIType : IComponentAPI<ParamType>
            where ParamType : IParameters
        {
            return Sequence<Action<APIType>>(
                api => api._raiseStop(),
                api => api._raisePostExecution(),
                api => api._raiseDispose()
            );
        }

        public static SyncInteractionQueue<APIType, ParamType> CompleteExecAfterEach<APIType, ParamType>(IEnumerable<Action<APIType>> synchronousEvents)
            where APIType : IComponentAPI<ParamType>
            where ParamType : IParameters
        {
            List<Action<APIType>> completeCycle = new List<Action<APIType>>();
            completeCycle.AddRange(LifecycleStart<APIType, ParamType>());
            foreach (var evt in synchronousEvents)
            {
                completeCycle.Add(evt);
                completeCycle.Add(api => api._raiseExecute());
            }
            completeCycle.AddRange(LifecycleEnd<APIType, ParamType>());
            return new SyncInteractionQueue<APIType, ParamType>(completeCycle);
        }
    }

    public abstract class ComponentSyncProgramRecord<APIType, ParamType>
        where APIType : IComponentAPI<ParamType>
        where ParamType : IParameters
    {

        public Logger LogBuffered = new BufferLogger();
        public HistoryBox<ComponentProgress> ProgressHistory = new HistoryBox<ComponentProgress>();

        public ComponentSyncProgramRecord(Func<IStandaloneComponent<APIType, ParamType>> programSpec, IComponentProgramInteraction<APIType, ParamType> interaction)
        {
            ProgramSpec = programSpec;
            Interaction = interaction;
        }

        public event Action<IStandaloneComponent<APIType, ParamType>> OnComponentCreated = (c) => { };
        public event Action OnComponentInteractionCompleted = () => { };

        public void Run()
        {
            ProgramInstance = ProgramSpec.Invoke();

            API.OnProgressChanged += ProgressHistory.Record;

            OnComponentCreated += (c) =>
            {
                c.api.OnLogMessage += (msg, level) => LogBuffered.Log(msg, level);
            };
            OnComponentCreated += (c) => ComponentCreated();

            API.OnProgressChanged += (progress) =>
            {
                if (progress.IsFinished) this.InteractionCompleted();
            };

            OnComponentCreated(ProgramInstance);
            Interaction.Interact(ProgramInstance, -1);
            OnComponentInteractionCompleted();
        }

        public virtual void ComponentCreated() { }
        public virtual void InteractionCompleted()
        {

        }

        public Func<IStandaloneComponent<APIType, ParamType>> ProgramSpec { get; }
        public IComponentProgramInteraction<APIType, ParamType> Interaction { get; }
        public IStandaloneComponent<APIType, ParamType> ProgramInstance { get; private set; }
        public APIType API => ProgramInstance.api;
    }
}

