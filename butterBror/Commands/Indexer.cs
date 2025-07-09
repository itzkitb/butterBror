using butterBror.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Bot.Console;

namespace butterBror
{
    public partial class Commands
    {
        private static readonly Dictionary<Type, Func<object>> _instanceFactories = [];
        private static readonly Dictionary<Type, MethodInfo> _methodCache = [];
        private static readonly Dictionary<Type, Delegate> _syncDelegates = [];
        private static readonly Dictionary<Type, Delegate> _asyncDelegates = [];

        /// <summary>
        /// Indexes all available commands and prepares execution handlers with reflection.
        /// </summary>
        /// <remarks>
        /// - Uses reflection to inspect command classes for runtime execution
        /// - Builds both synchronous and asynchronous execution delegates
        /// - Registers command handlers for later use in command processing
        /// - Handles errors during command indexing with warning logging
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "IndexCommands")]
        public static void IndexCommands()
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Indexing commands...", "info");

            foreach (var classType in commands)
            {
                try
                {
                    var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                    var info = infoProperty.GetValue(null) as CommandInfo;

                    _instanceFactories[classType] = CreateInstanceFactory(classType);
                    var method = classType.GetMethod("Index", BindingFlags.Public | BindingFlags.Instance);
                    _methodCache[classType] = method;

                    Delegate syncDelegate = null;
                    Delegate asyncDelegate = null;

                    if (method.ReturnType == typeof(CommandReturn))
                    {
                        syncDelegate = CreateSyncDelegate(classType, method);
                        _syncDelegates[classType] = syncDelegate;
                    }
                    else if (method.ReturnType == typeof(Task<CommandReturn>))
                    {
                        asyncDelegate = CreateAsyncDelegate(classType, method);
                        _asyncDelegates[classType] = asyncDelegate;
                    }

                    lock (_handlersLock)
                    {
                        var handler = new CommandHandler
                        {
                            Info = info,
                            sync_executor = null,
                            async_executor = null
                        };

                        if (syncDelegate != null)
                        {
                            handler.sync_executor = (data) =>
                            {
                                var instance = _instanceFactories[classType]();
                                return (CommandReturn)syncDelegate.DynamicInvoke(instance, data);
                            };
                        }
                        else if (asyncDelegate != null)
                        {
                            handler.async_executor = async (data) =>
                            {
                                var instance = _instanceFactories[classType]();
                                return await (Task<CommandReturn>)asyncDelegate.DynamicInvoke(instance, data);
                            };
                        }

                        commandHandlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    Write($"[COMMAND_INDEXER] INDEX ERROR FOR CLASS {classType.Name}: {ex.Message}\n{ex.StackTrace}", "info", LogLevel.Warning);
                }
            }
            Write($"Indexed! ({commandHandlers.Count} commands loaded)", "info");
        }

        /// <summary>
        /// Creates a factory delegate to instantiate command objects.
        /// </summary>
        /// <param name="type">The command type to create a factory for.</param>
        /// <returns>A Func<object> delegate that creates new instances of the specified type.</returns>
        /// <exception cref="MissingMethodException">Thrown when no parameterless constructor exists.</exception>
        /// <remarks>
        /// Uses expression trees for efficient instance creation.
        /// Ensures command types have default constructors.
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "CreateInstanceFactory")]
        private static Func<object> CreateInstanceFactory(Type type)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new MissingMethodException($"No parameterless constructor found for {type.Name}");

                var newExpr = Expression.New(constructor);
                var lambda = Expression.Lambda<Func<object>>(newExpr);
                return lambda.Compile();
            }
            catch (Exception ex)
            {
                Write($"Failed to create factory for {type.Name}: {ex.Message}", "info");
                throw;
            }
        }

        /// <summary>
        /// Creates a synchronous execution delegate for command methods.
        /// </summary>
        /// <param name="type">The command type containing the method.</param>
        /// <param name="method">The MethodInfo of the command's execution method.</param>
        /// <returns>A Delegate that can synchronously execute the command.</returns>
        /// <remarks>
        /// Builds expression-based invocation for performance.
        /// Converts object instances to target command type before method call.
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "CreateSyncDelegate")]
        private static Delegate CreateSyncDelegate(Type type, MethodInfo method)
        {
            Engine.Statistics.FunctionsUsed.Add();
            var instanceParam = Expression.Parameter(typeof(object));
            var dataParam = Expression.Parameter(typeof(CommandData));
            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                dataParam
            );
            return Expression.Lambda<Func<object, CommandData, CommandReturn>>(call, instanceParam, dataParam).Compile();
        }

        /// <summary>
        /// Creates an asynchronous execution delegate for command methods.
        /// </summary>
        /// <param name="type">The command type containing the async method.</param>
        /// <param name="method">The MethodInfo of the async command method.</param>
        /// <returns>A Delegate that can asynchronously execute the command.</returns>
        /// <remarks>
        /// Builds expression-based async invocation.
        /// Handles Task<CommandReturn> return types.
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "CreateAsyncDelegate")]
        private static Delegate CreateAsyncDelegate(Type type, MethodInfo method)
        {
            Engine.Statistics.FunctionsUsed.Add();
            var instanceParam = Expression.Parameter(typeof(object));
            var dataParam = Expression.Parameter(typeof(CommandData));
            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                dataParam
            );
            return Expression.Lambda<Func<object, CommandData, Task<CommandReturn>>>(call, instanceParam, dataParam).Compile();
        }
    }
}
