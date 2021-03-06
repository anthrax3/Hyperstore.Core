//	Copyright � 2013 - 2014, Alain Metge. All rights reserved.
//
//		This file is part of Hyperstore (http://www.hyperstore.org)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hyperstore.Modeling.Commands;
using Hyperstore.Modeling.Events;
using Hyperstore.Modeling.Utils;
using Hyperstore.Modeling.Platform;
using Hyperstore.Modeling.Metadata.Constraints;

#endregion


namespace Hyperstore.Modeling
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///  A session.
    /// </summary>
    /// <seealso cref="T:Hyperstore.Modeling.ISession"/>
    /// <seealso cref="T:Hyperstore.Modeling.ISessionContext"/>
    /// <seealso cref="T:Hyperstore.Modeling.ISessionInternal"/>
    ///-------------------------------------------------------------------------------------------------
    public class Session : ISession, ISessionContext, ISessionInternal, ISupportsCalculatedPropertiesTracking
    {
        /// <summary>
        ///     Gestion des num�ros de session.
        ///     Chaque session a un identifiant (SessionIndex) unique qui r�f�rence un contexte dans un dictionnaire
        ///     Pour �viter que le dictionnaire grossisse exag�r�ment, on va optimiser la gestion des index en r�utilisant
        ///     les index des sessions termin�es.
        ///     On conserve la liste des index utilis�s dans un tableau de bit. Si le bit correspondant � un index
        ///     est � 1, l'index est utilis� et ne peut pas �tre affect� � une nouvelle session.
        /// </summary>
        private static readonly SessionIndexProvider s_sessionSequences = new SessionIndexProvider();
        private static int s_sessionIdSequences = 0;

        // Identifiant de session (stock� au niveau du thread)
        // Doit �tre initialis� pour chaque thread soit par la cr�ation d'une session, soit par using(new MultiThreadSession)
        [ThreadStatic]
        private static ushort? _sessionIndex;
        private readonly IHyperstoreTrace _trace;
        private ITransactionScope _scope; // scope est lui aussi li� au thread

        static Session()
        {
            SessionContexts = PlatformServices.Current.CreateConcurrentDictionary<UInt16, SessionDataContext>();
        }

        /// <summary>
        ///     Constructeur interne. Il est appel� par le store quand il cr�e la session
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="cfg">The CFG.</param>
        internal Session(IHyperstore store, SessionConfiguration cfg)
        {
            DebugContract.Requires(store, "store");

            if (SessionIndex == null)
                SessionIndex = s_sessionSequences.GetFirstFreeValue();

            _trace = store.Trace;

            var ctx = SessionDataContext;
            if (ctx == null)
            {
                // _statSessionCount.Incr();

                // Nouvelle session
                ctx = new SessionDataContext // TODO optimize memory size
                      {
                          TrackingData = new SessionTrackingData(this),
                          SessionIsolationLevel = cfg.IsolationLevel,
                          Locks = new List<ILockInfo>(),
                          ReadOnly = cfg.Readonly,
                          ReadOnlyStatus = cfg.Readonly,
                          Current = this,
                          OriginStoreId = cfg.Origin ?? store.Id,
                          Mode = cfg.Mode,
                          SessionId = cfg.SessionId != 0 ? cfg.SessionId : Interlocked.Increment(ref s_sessionIdSequences),
                          Store = store,
                          CancellationToken = cfg.CancellationToken,
                          Enlistment = new List<ISessionEnlistmentNotification>(),
                          SessionInfos = new Stack<SessionLocalInfo>(),
                          TopLevelSession = this,
                          DefaultDomainModel = cfg.DefaultDomainModel,
                      };

                SessionDataContext = ctx;

                _scope = Hyperstore.Modeling.Platform.PlatformServices.Current.CreateTransactionScope(this, cfg);
            }
            else if (ctx.SessionInfos.Count == 0)
                throw new HyperstoreException(ExceptionMessages.CannotCreateNestedSessionInDisposingSession);

            ctx.SessionInfos.Push(new SessionLocalInfo
                                  {
                                      DefaultDomainModel = cfg.DefaultDomainModel ?? ctx.DefaultDomainModel,
                                      Mode = cfg.Mode | ctx.Mode,
                                      OriginStoreId = cfg.Origin ?? ctx.OriginStoreId
                                  });
        }

        internal static ushort? SessionIndex
        {
            get { return _sessionIndex; }
            set
            {
                DebugContract.Requires(value != null);
                _sessionIndex = value;
            }
        }

        //#if NETFX_CORE

        //        internal static ushort? SessionIndex
        //        {
        //            get
        //            {
        //                var ctx = SynchronizationContext.Current as HyperstoreSynchronizationContext;
        //                if (ctx == null)
        //                {
        //                    return null;
        //                }
        //                return ctx.SessionIndex;
        //            }
        //            set
        //            {
        //                if (value == null)
        //                {
        //                    var ctx = SynchronizationContext.Current as HyperstoreSynchronizationContext;
        //                    _sessionSequences.ReleaseValue(SessionIndex.Value);
        //                    SynchronizationContext.SetSynchronizationContext(ctx.OldContext);
        //                }
        //                else
        //                {
        //                    new HyperstoreSynchronizationContext(value.Value, SynchronizationContext.Current);
        //                }
        //            }
        //        }
        //#else
        //        private static readonly string SessionIndexKey = Guid.NewGuid().ToString("N");
        //  TODO : A remettre quand TOUS les frameworks supporteront le LogicalGetData (Penser � modifier le TransactionScopeAsyncFlowOption dans le TransactionScopeWrapper)
        //        internal static ushort? SessionIndex
        //        {
        //            get 
        //            {
        //                return System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(SessionIndexKey) as Nullable<ushort>;
        //            }
        //            set 
        //            {
        //                if (value == null)
        //                {
        //                    _sessionSequences.ReleaseValue(SessionIndex.Value);
        //                }
        //                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SessionIndexKey, value);
        //            }
        //        }
        //#endif

        private static SessionDataContext SessionDataContext
        {
            get
            {
                var ix = SessionIndex;
                if (ix == null)
                    return null;

                SessionDataContext ctx;
                SessionContexts.TryGetValue(ix.Value, out ctx);
                return ctx;
            }
            set
            {
                var ix = SessionIndex;
                if (ix != null)
                    SessionContexts[ix.Value] = value;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the current.
        /// </summary>
        /// <value>
        ///  The current.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public static ISession Current
        {
            get
            {
                var ctx = SessionDataContext;
                return ctx != null ? ctx.Current : null;
            }
        }

        internal List<ISessionEnlistmentNotification> Enlistment
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null)
                    return null;
                return ctx.Enlistment;
            }
        }

        /// <summary>
        ///     Liste des �l�ments impact�s par les commandes lors de la session. Si plusieurs commandes op�rent sur un m�me
        ///     �l�ment, il
        ///     ne sera r�pertori� qu'une fois.
        /// </summary>
        /// <value>
        ///     The involved elements.
        /// </value>
        private IEnumerable<IModelElement> InvolvedElements
        {
            get
            {
                var trackingData = SessionDataContext.TrackingData;
                return trackingData.InvolvedModelElements;
            }
        }
        //public IEnumerable<DiagnosticMessage> Messages
        //{
        //    get { return SessionDataContext.MessageList.Messages; }
        //}

        //public bool HasErrors
        //{
        //    get { return SessionDataContext.MessageList.HasErrors; }
        //}

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Session terminated event.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public event EventHandler<SessionCompletingEventArgs> Completing;

        void ISessionInternal.PushExecutionScope()
        {
            var ctx = SessionDataContext;
            ctx.CommandExecutionScopeLevel++;
        }

        void ISessionInternal.PopExecutionScope()
        {
            var ctx = SessionDataContext;
            ctx.CommandExecutionScopeLevel--;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the store.
        /// </summary>
        /// <value>
        ///  The store.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public IHyperstore Store
        {
            get { return SessionDataContext.Store; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the default domain model.
        /// </summary>
        /// <value>
        ///  The default domain model.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public IDomainModel DefaultDomainModel
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null || ctx.Depth == 0)
                    return null;

                return ctx.SessionInfos.Peek().DefaultDomainModel;
            }
        }


        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the top level session.
        /// </summary>
        /// <value>
        ///  The top level session.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public ISession TopLevelSession
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null || ctx.Depth == 0)
                    return null;
                return ctx.TopLevelSession;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        ///  true if this instance is read only, false if not.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool IsReadOnly
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null)
                    return true;
                return ctx.ReadOnlyStatus;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the identifier of the origin store.
        /// </summary>
        /// <value>
        ///  The identifier of the origin store.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public string OriginStoreId
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null)
                    return null;
                return ctx.SessionInfos.Peek().OriginStoreId;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets information describing the tracking.
        /// </summary>
        /// <value>
        ///  Information describing the tracking.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public ISessionTrackingData TrackingData
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null)
                    return null;
                return ctx.TrackingData;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the cancellation token.
        /// </summary>
        /// <value>
        ///  The cancellation token.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public CancellationToken CancellationToken
        {
            get
            {
                var ctx = SessionDataContext;
                return ctx.CancellationToken;
            }
        }

        ICollection<ILockInfo> ISession.Locks
        {
            get
            {
                var ctx = SessionDataContext;
                return ctx.Locks;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the isolation level.
        /// </summary>
        /// <value>
        ///  The isolation level.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public SessionIsolationLevel SessionIsolationLevel
        {
            get
            {
                var ctx = SessionDataContext;
                return ctx.SessionIsolationLevel;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets a value indicating whether this instance is nested.
        /// </summary>
        /// <value>
        ///  true if this instance is nested, false if not.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool IsNested
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null || ctx.Depth == 0)
                    return false;
                return ctx.Depth > 1;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets a value indicating whether this session is disposing.
        /// </summary>
        /// <value>
        ///  <c>true</c> if this instance is disposing; otherwise, <c>false</c>.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool IsDisposing
        {
            get
            {
                var ctx = SessionDataContext;
                return ctx.Disposing;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets a value indicating whether this instance is aborted.
        /// </summary>
        /// <value>
        ///  true if this instance is aborted, false if not.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool IsAborted
        {
            get
            {
                var ctx = SessionDataContext;
                return ctx.Aborted;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Processes the command.
        /// </summary>
        /// <exception cref="Exception">
        ///  Thrown when an exception error condition occurs.
        /// </exception>
        /// <exception cref="SessionException">
        ///  Thrown when a Session error condition occurs.
        /// </exception>
        /// <param name="commands">
        ///  A variable-length parameters list containing command.
        /// </param>
        /// <returns>
        ///  An IExecutionResult.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public ISessionResult Execute(params IDomainCommand[] commands)
        {
            if (commands == null || commands.Length == 0)
                return ExecutionResult.Empty;

            var domainCommands = commands.Where(cmd => cmd != null).ToList();
            if (domainCommands.Count == 0)
                return ExecutionResult.Empty;

            var result = new ExecutionResult();
            var commandsByDomains = from cmd in domainCommands group cmd by cmd.DomainModel.Name;

            ((ISessionInternal)this).PushExecutionScope();

            try
            {
                foreach (var cd in commandsByDomains)
                {
                    var dm = Store.GetDomainModel(cd.Key);
                    if (dm == null)
                        dm = Store.GetSchema(cd.Key);
                    if (dm == null)
                        throw new DomainNotFoundException(cd.Key);

                    if (dm is IUpdatableDomainModel)
                    {
                        var messages = ((IUpdatableDomainModel)dm).Commands.ProcessCommands(cd.ToArray());
                        result.AddMessages(messages);
                    }
                }
            }
            catch (SessionException)
            {
                // On force le rollback sur la session car l'appel courant a pu �tre int�gr� dans une session 
                // englobante          
                ((ISessionInternal)this).RejectChanges();
            }
            catch (Exception ex) // Si bug ds Hyperstore ;-)
            {
                result.AddMessage(new DiagnosticMessage(MessageType.Error, "Command aborted", "Fatal error", ex: ex));
                // On force le rollback sur la session car l'appel courant a pu �tre int�gr� dans une session 
                // englobante          
                ((ISessionInternal)this).RejectChanges();
            }
            finally
            {
                ((ISessionInternal)this).PopExecutionScope();
                // Sinon si on est dans une session englobante, il faut g�n�rer directement l'exception.
                if (result.ShouldRaiseException() && (((ISessionInternal)this).Mode & SessionMode.SilentMode) != SessionMode.SilentMode)
                    throw new SessionException(result.Messages);
            }
            return result;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Acquires a lock for a property.
        /// </summary>
        /// <param name="mode">
        ///  The mode.
        /// </param>
        /// <param name="id">
        ///  The identifier.
        /// </param>
        /// <param name="propertyName">
        ///  (Optional) name of the property.
        /// </param>
        /// <returns>
        ///  An IDisposable.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public IDisposable AcquireLock(LockType mode, Identity id, string propertyName = null)
        {
            Contract.Requires(id, "id");
            var ressource = id;
            if (!String.IsNullOrWhiteSpace(propertyName))
                ressource = id.CreateAttributeIdentity(propertyName);
            return AcquireLock(mode, ressource.ToString());
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Acquires a lock.
        /// </summary>
        /// <param name="mode">
        ///  The mode.
        /// </param>
        /// <param name="ressource">
        ///  The ressource.
        /// </param>
        /// <returns>
        ///  An IDisposable.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public IDisposable AcquireLock(LockType mode, object ressource)
        {
            Contract.Requires(ressource, "ressource");

            return Store.LockManager.AcquireLock(this, ressource, mode);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Enlists the specified transaction.
        /// </summary>
        /// <param name="transaction">
        ///  The transaction.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public void Enlist(ITransaction transaction)
        {
            if (_scope != null)
                _scope.Enlist(transaction);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets context information.
        /// </summary>
        /// <typeparam name="T">
        ///  Generic type parameter.
        /// </typeparam>
        /// <param name="key">
        ///  The key.
        /// </param>
        /// <returns>
        ///  The context information.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public T GetContextInfo<T>(string key)
        {
            Contract.RequiresNotEmpty(key, "key");

            var ctx = SessionDataContext;
            if (ctx == null || ctx.Depth == 0)
                return default(T);

            foreach (var s in ctx.SessionInfos.Reverse())
            {
                var infos = s.Infos;

                object obj;
                if (infos.TryGetValue(key, out obj))
                    return (T)obj;
            }

            return default(T);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Sets context information.
        /// </summary>
        /// <param name="key">
        ///  The key.
        /// </param>
        /// <param name="value">
        ///  The value.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public void SetContextInfo(string key, object value)
        {
            Contract.RequiresNotEmpty(key, "key");
            var ctx = SessionDataContext;
            if (ctx == null || ctx.Depth == 0)
                return;

            var infos = ctx.SessionInfos.Peek().Infos;
            if (value == null)
            {
                if (infos.ContainsKey(key))
                    infos.Remove(key);
            }
            else
                infos[key] = value;
        }

        void ISession.SetMode(SessionMode mode)
        {
            var ctx = SessionDataContext;
            if (ctx == null || ctx.Depth == 0)
                return;

            ctx.SessionInfos.Peek().Mode |= mode;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the mode.
        /// </summary>
        /// <value>
        ///  The mode.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public SessionMode Mode
        {
            get
            {
                var ctx = SessionDataContext;
                if (ctx == null || ctx.Depth == 0)
                    return SessionMode.Normal;

                return ctx.SessionInfos.Peek().Mode;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Suppression de la session. C'est dans cette m�thode que seront envoy�s les notifications, que
        ///  s'effectueront les validations si la transaction est une transaction racine.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Accepts the changes.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public void AcceptChanges()
        {
            var state = SessionDataContext.SessionInfos.Peek();
            state.Committed = true;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Rejects the session (Abort).
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public void RejectChanges()
        {
            SessionDataContext.Aborted = true;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Adds an event to the session events list.
        /// </summary>
        /// <exception cref="ReadOnlyException">
        ///  Thrown when a Read Only error condition occurs.
        /// </exception>
        /// <param name="event">
        ///  The event.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public void AddEvent(IEvent @event)
        {
            Contract.Requires(@event, "@event");

            if (SessionDataContext.ReadOnly)
                throw new ReadOnlyException("Read only session can not be modified");

            @event.TopEvent = IsInTopLevelCommandScope();
            SessionDataContext.Events.Add(@event);
            SessionDataContext.TrackingData.OnEvent(@event);

            foreach (var notifier in GetNotifiers())
            {
                notifier.NotifyEvent(this, SessionContext, @event);
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the events.
        /// </summary>
        /// <value>
        ///  The events.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public IEnumerable<IEvent> Events
        {
            get { return SessionDataContext.Events; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the identifier of the session.
        /// </summary>
        /// <value>
        ///  The identifier of the session.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public int SessionId
        {
            get { return SessionDataContext.SessionId; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the session context.
        /// </summary>
        /// <value>
        ///  The session context.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public ISessionContext SessionContext
        {
            get { return this; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Logs the specified message.
        /// </summary>
        /// <param name="message">
        ///  The message.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public void Log(DiagnosticMessage message)
        {
            Contract.Requires(message, "message");
            SessionDataContext.MessageList.AddMessage(message);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the result.
        /// </summary>
        /// <value>
        ///  The result.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public ISessionResult Result
        {
            get { return SessionDataContext != null ? SessionDataContext.MessageList : null; }
        }

        internal static void ResetSessionIndex(bool releaseIndex)
        {
            if (releaseIndex)
                s_sessionSequences.ReleaseValue(SessionIndex.Value);
            _sessionIndex = null;
        }

        private bool IsInTopLevelCommandScope()
        {
            var ctx = SessionDataContext;
            return ctx.CommandExecutionScopeLevel == 1;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Suppression de la session. C'est dans cette m�thode que seront envoy�s les notifications, que
        ///  s'effectueront les validations si la transaction est une transaction racine.
        /// </summary>
        /// <param name="disposing">
        ///  .
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        protected void Dispose(bool disposing)
        {
            var ctx = SessionDataContext;
            if (ctx == null)
                return;

            var currentMode = Mode;
            var currentInfo = ctx.SessionInfos.Pop();

            // Si la transaction n'a pas �t� valid�e, toutes les transactions englobantes deviennent invalides
            if (!currentInfo.Committed && !ctx.ReadOnly || ctx.CancellationToken.IsCancellationRequested)
                ctx.Aborted = true;

            // Top transaction
            if (ctx.Depth == 0)
            {
                System.Diagnostics.Debug.Assert(ctx.Trackers == null || ctx.Trackers.Count == 0);
                using (CodeMarker.MarkBlock("Session.Dispose"))
                {
                    var notifiers = GetNotifiers();

                    var result = CompleteTopLevelTransaction(notifiers, ctx, currentInfo);

                    if (!ctx.CancellationToken.IsCancellationRequested)
                        NotifyDiagnosticMessages(disposing, currentMode, notifiers, result);
                }
            }
        }

        private void NotifyDiagnosticMessages(bool disposing, SessionMode currentMode, IEnumerable<IEventNotifier> notifiers, Tuple<ExecutionResult, ISessionInformation> result)
        {
            var messages = result.Item1;
            try
            {
                if (result.Item2 != null) // Finalize
                {
                    // Enfin notification des erreurs
                    // Cette partie ne doit pas planter toute erreur sera ignor�e
                    using (CodeMarker.MarkBlock("Session.Notifications"))
                    {
                        foreach (var notifier in notifiers)
                        {
                            notifier.NotifyMessages(result.Item2, messages);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                messages.AddMessage(new DiagnosticMessage(MessageType.Error, ex.Message, "Session", SessionDataContext.InValidationProcess, null, ex));
                _trace.WriteTrace(TraceCategory.Session, ExceptionMessages.Trace_FatalErrorInEventsNotificationProcessFormat, ex.Message);
            }
            finally
            {
                if (disposing && messages.ShouldRaiseException() && (currentMode & SessionMode.SilentMode) != SessionMode.SilentMode)
                    throw new SessionException(messages.Messages);
            }
        }

        private Tuple<ExecutionResult, ISessionInformation> CompleteTopLevelTransaction(IEnumerable<IEventNotifier> notifiers, SessionDataContext ctx, SessionLocalInfo currentInfo)
        {
            // Sauvegarde des r�f�rences vers les objets qui sont utilis�s apr�s que les donn�es de la session auront �t� supprim�es
            ExecutionResult messages = null;
            ISessionInformation sessionInfo = null;

            // Il ne peut pas y avoir d'erreur dans cette partie de code
            try
            {
                ctx.Disposing = true;

                // Si la session �tait en lecture seule, on simplifie les traitements
                // Pas de validation
                if (!ctx.ReadOnly)
                {
                    messages = ExecuteConstraints(ctx, currentInfo);
                }

                // Contexte en lecture seule de la session mais tjs dans le scope
                // Envoi des events m�me en read-only pour s'assurer que le OnSessionCompleted soit bien notifi�
                if (!ctx.CancellationToken.IsCancellationRequested)
                {
                    using (CodeMarker.MarkBlock("Session.OnSessionCompleted"))
                    {
                        sessionInfo = OnSessionCompleted(currentInfo, notifiers, messages);
                    }
                }

                // Si tout va bien, on commite
                if (!IsAborted && _scope != null && (messages == null || !messages.HasErrors))
                    _scope.Complete();
            }
            catch (Exception ex)
            {
                Log(new DiagnosticMessage(MessageType.Error, ex.Message, ExceptionMessages.Diagnostic_ApplicationError, SessionDataContext.InValidationProcess, null, ex));
            }
            finally
            {
                DisposeSession(ctx);
            }

            return Tuple.Create(messages ?? ExecutionResult.Empty, sessionInfo);
        }

        private ExecutionResult ExecuteConstraints(SessionDataContext ctx, SessionLocalInfo currentInfo)
        {
            var messages = ctx.MessageList;

            // Notification des �v�nements.
            // La notification se fait encore dans le scope actif

            // A partir d'ici, les �v�nements (issues des commandes) NE SONT PLUS pris en compte car si une nouvelle commande est �mise
            // il faut pouvoir :
            //  1 - ex�cuter les contraintes sur les nouvelles modifications.
            //  2 - renvoyer les nouveaux �vents.
            // Ce qui n'est pas possible � partir d'ici puisqu'on est dans le processus g�rant ces cas.
            //
            // Si on veut g�n�rer d'autres events, il faut s'abonner dans un autre thread (pour pouvoir cr�er une nouvelle session)
            ctx.ReadOnly = true;
            var hasInvolvedElements = false;
            using (CodeMarker.MarkBlock("Session.PrepareInvolvedElements"))
            {
                // R�cup�ration des �l�ments impact�s au cours de la session
                // Ne pas le faire lors des chargements des metadonn�es  car :
                //  1 - Il n'y a pas de contraintes sur les metadonn�es
                //  2 - En cas de chargement d'une extension, les m�tadonn�es ne sont pas encore accessibles et cela fait planter le code.
                hasInvolvedElements = SessionDataContext.TrackingData.PrepareModelElements(IsAborted, (currentInfo.Mode & SessionMode.LoadingSchema) == SessionMode.LoadingSchema);
            }

            // Validation implicite sur les �l�ments modifi�s au cours de la session
            // Ce code ne doit pas se faire lorsqu'on charge les metadonn�es
            if (hasInvolvedElements && (currentInfo.Mode & SessionMode.SkipConstraints) != SessionMode.SkipConstraints)
            {
                try
                {
                    // V�rification des contraintes implicites
                    using (CodeMarker.MarkBlock("Session.CheckConstraints"))
                    {
                        CheckConstraints(InvolvedElements);
                    }
                    // Une contrainte peut entrainer un rollback de la transaction
                    // si il retourne une erreur dans result.
                    ctx.Aborted = ctx.MessageList != null && ctx.MessageList.HasErrors;
                }
                catch (Exception ex)
                {
                    ctx.Aborted = true;
                    var exception = ex;
                    if (exception is AggregateException)
                        exception = ((AggregateException)ex).InnerException;
                    Log(new DiagnosticMessage(MessageType.Error, exception.Message, ExceptionMessages.Diagnostic_ConstraintsProcessError, SessionDataContext.InValidationProcess, null, exception));
                }
            }
            return messages;
        }

        private void DisposeSession(SessionDataContext ctx)
        {

            try
            {
                if (_scope != null)
                {
                    using (CodeMarker.MarkBlock("Session.ScopeDispose"))
                    {
                        _scope.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                var exception = ex;
                if (exception is AggregateException)
                    exception = ((AggregateException)ex).InnerException;
                Log(new DiagnosticMessage(MessageType.Error, exception.Message, "Session", SessionDataContext.InValidationProcess, null, exception));
            }
            _scope = null;

            try
            {
                // Lib�ration des locks                    
                Store.LockManager.ReleaseLocks(ctx.Locks, ctx.Aborted);
            }
            catch (Exception ex)
            {
                throw new CriticalException(ExceptionMessages.CriticalErrorMaybeAwaitInSession, ex);
            }

            // Suppression de la session courante. Il n'est plus possible de faire r�f�rence � la session
            ctx.Current = null;
            SessionDataContext = null;
            ResetSessionIndex(true);
        }

        private List<IEventNotifier> GetNotifiers()
        {
            var extensions = Store as IDomainManager;
            if (extensions == null)
                throw new CriticalException("Store must implement IExtensionManager");
            return extensions.GetEventsNotifiers();
        }

        // V�rification des contraintes implicites.
        // Ces contraintes ne portent que sur les �l�ments impact�s lors de l'ex�cution de la session.
        private void CheckConstraints(IEnumerable<IModelElement> involvedElements)
        {
            var constraints =
                involvedElements
                .Where(e => e.SchemaInfo.Schema.Constraints is IConstraintManagerInternal && (e.SchemaInfo.Schema.Constraints as IConstraintManagerInternal).HasImplicitConstraints)
                .GroupBy(e => e.SchemaInfo.Schema.Constraints as IConstraintManagerInternal);

            if (!constraints.Any())
                return;

            try
            {
                SessionDataContext.InValidationProcess = true;
                var idx = SessionIndex;
                var threadId = ThreadHelper.CurrentThreadId;

                PlatformServices.Current.Parallel_ForEach(constraints, group =>
                {
                    using (new MultiThreadedSession(idx.Value, threadId)) // Partage de session
                    {
                        group.Key.CheckElements(group);
                    }
                });
            }
            finally
            {
                SessionDataContext.InValidationProcess = false;
            }
        }

        /// <summary>
        ///     Notification de la fin de la session
        /// </summary>
        private ISessionInformation OnSessionCompleted(SessionLocalInfo info, IEnumerable<IEventNotifier> notifiers, IExecutionResultInternal messages)
        {
            DebugContract.Requires(notifiers != null, "notifiers");
            // On fait une copie de la session car les �v�nements peuvent 
            // �tre souscrits dans un autre thread
            var sessionContext = new SessionInformation(this, info, SessionDataContext.TrackingData, messages);

            try
            {
                // Notifications via RX.
                foreach (var notifier in notifiers)
                {
                    notifier.NotifySessionCompleted(sessionContext, SessionContext);
                }

                // D�clenchement des �v�nements
                // D'abord �venement hard
                var tmp = Completing;
                if (tmp != null)
                    tmp(this, new SessionCompletingEventArgs(sessionContext));
            }
            catch (Exception ex)
            {
                // Si une erreur survient dans une notification, on l'intercepte
                Log(new DiagnosticMessage(MessageType.Error, ex.Message, "SessionTerminated", SessionDataContext.InValidationProcess, null, ex));
            }
            return sessionContext;
        }

        private class MultiThreadedSession : IDisposable
        {
            private readonly int _threadId;

            internal MultiThreadedSession(ushort? sessionIndex, int threadId)
            {
                DebugContract.Requires(sessionIndex > 0);
                SessionIndex = sessionIndex;
                _threadId = threadId;
            }

            ///-------------------------------------------------------------------------------------------------
            /// <summary>
            ///  Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
            ///  resources.
            /// </summary>
            ///-------------------------------------------------------------------------------------------------
            public void Dispose()
            {
                if (_threadId != ThreadHelper.CurrentThreadId)
                    ResetSessionIndex(false);
            }
        }

        #region Data thread

        // Les donn�es de session sont partag�s entre tous les threads afin de pouvoir partager une session sur plusieurs threads.
        // Ceci est possible en propageant le num�ro de session entre les threads (SessionIndex)
        // Il est � noter que le TransactionScope est li� � un thread et ne peut pas �tre partag�, c'est pour cela que le partage de session
        // doit se faire en mode read only au niveau de la session.
        // 
        // Ce m�canisme est utilis� pour lancer les contraintes en // (voir CheckConstraints)
        //
        // Les donn�es sont partag�es en utilisant la m�thode GetContextInfo<> de la session courante.
        //
        // Donn�es de session partag�es
        private static readonly IConcurrentDictionary<UInt16, SessionDataContext> SessionContexts;

        #endregion

        IDisposable ISupportsCalculatedPropertiesTracking.PushCalculatedPropertyTracker(CalculatedProperty tracker)
        {
            var ctx = SessionDataContext;
            if (ctx == null)
                return Disposables.Empty;

            if (ctx.Trackers == null)
                ctx.Trackers = new Stack<CalculatedProperty>();

            ctx.Trackers.Push(tracker);


            return Disposables.ExecuteOnDispose(() => SessionDataContext.Trackers.Pop());
        }

        CalculatedProperty ISupportsCalculatedPropertiesTracking.CurrentTracker
        {
            get
            {
                var ctx = SessionDataContext;
                var trackers = ctx != null ? ctx.Trackers : null;
                return trackers != null && trackers.Count > 0 ? trackers.Peek() : null;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Session has completed correctly
        /// </summary>
        /// <value>
        ///  true if succeed, false if not.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool Succeed
        {
            get
            {
                var r = Result; if (r == null)
                    return false;
                return !(r.HasErrors || r.HasWarnings || IsAborted);
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets a value indicating whether this instance has errors.
        /// </summary>
        /// <value>
        ///  true if this instance has errors, false if not.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool HasErrors
        {
            get { return Result != null ? Result.HasErrors : false; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets a value indicating whether this instance has warnings.
        /// </summary>
        /// <value>
        ///  true if this instance has warnings, false if not.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public bool HasWarnings
        {
            get { return Result != null ? Result.HasWarnings : false; }
        }
    }
}