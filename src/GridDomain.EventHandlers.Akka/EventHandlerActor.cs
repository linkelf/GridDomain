using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace GridDomain.EventHandlers.Akka
{
    public static class EventHandlerActor
    {
        public class Start
        {
            private Start()
            {
            }

            public static readonly Start Instance = new Start();
        }

        public class Next
        {
            private Next()
            {
            }

            public static readonly Next Instance = new Next();
        }

        public class Done
        {
            private Done()
            {
            }

            public static readonly Done Instance = new Done();
        }
    }
    
    public class EventHandlerActor<TMessage,THandler> : ReceiveActor where THandler:IEventHandler<TMessage>
    {
        public EventHandlerActor()
        {
            var log = Context.GetLogger();
            var handler = Context.System.GetEventHandlersExtension().GetHandler<THandler>();
            Receive<EventHandlerActor.Start>(s =>
            {
                log.Info("Starting projection");
                Sender.Tell(EventHandlerActor.Next.Instance);
            });
            Receive<EventHandlerActor.Done>(s =>
            {
                log.Info("Stopping projection");
            });
            Receive<Sequenced<TMessage>>(m =>
            {
                handler.Handle(new []{m}).ContinueWith(t => EventHandlerActor.Next.Instance).PipeTo(Sender);
            });
            
            ReceiveAny(o => log.Warning("missing message: " + o.ToString()));
        }

    }
    
    public class EventHandlerActor<TMessageA,TMessageB,THandler> : ReceiveActor where THandler:IEventHandler<TMessageA>, IEventHandler<TMessageB>
    {
        public EventHandlerActor()
        {
            var log = Context.GetLogger();
            var handler = Context.System.GetEventHandlersExtension().GetHandler<THandler>();
            Receive<EventHandlerActor.Start>(s =>
            {
                log.Info("Starting projection");
                Sender.Tell(EventHandlerActor.Next.Instance);
            });
            Receive<EventHandlerActor.Done>(s =>
            {
                log.Info("Stopping projection");
            });
            Receive<Sequenced>(m =>
            {
                Task handleTask;
                switch (m.Message)
                {
                   case TMessageA msg:
                       handleTask = handler.Handle(new[] {new Sequenced<TMessageA>(msg,m.Sequence)});
                       break; 
                   case TMessageB msg:
                       handleTask = handler.Handle(new[] {new Sequenced<TMessageB>(msg,m.Sequence)});
                       break;
                   default:
                       throw new UnknownEventReceivedException();
                }
                handleTask.ContinueWith(t => EventHandlerActor.Next.Instance).PipeTo(Sender);
            });
            
            ReceiveAny(o => log.Warning("missing message: " + o.ToString()));
        }

        public class UnknownEventReceivedException : Exception
        {
        }
    }
}