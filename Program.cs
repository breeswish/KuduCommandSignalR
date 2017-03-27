using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNet.SignalR.Client;

namespace KuduCommandSignalR
{
    class KuduMessage
    {
        /// <summary>
        /// Stdout
        /// </summary>
        public string Output { get; set; }

        public int RunningProcessesCount { get; set; }

        public int ProcessId { get; set; }

        /// <summary>
        /// Stderr
        /// </summary>
        public string Error { get; set; }

        public string Data
        {
            get
            {
                if (Output != null)
                {
                    return Output;
                }
                else if (Error != null)
                {
                    return Error;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public class Program
    {
        private static string argvHost, argvUsername, argvPassword, argvCommand;
        private static bool optDebugTrace = false;

        private static Connection _connection;
        private static ManualResetEvent _closedEvent = new ManualResetEvent(false);
        private static event EventHandler<KuduMessage> _commandReadyEvent;
        private static event EventHandler<String> _outputLineReceivedEvent;
        private static int _ready = 0;

        private static async Task SignalRTask()
        {
            _connection = new Connection($"https://{argvHost}/api/commandstream", "shell=CMD");

            _connection.Credentials = new NetworkCredential(argvUsername, argvPassword);
            _connection.Error += CommandError;
            _connection.Received += DataReceived;
            _connection.Closed += ConnectionClosed;

            if (optDebugTrace)
            {
                _connection.TraceLevel = TraceLevels.All;
                _connection.TraceWriter = Console.Out;
                _connection.StateChanged += ConnectionStateChanged;
            }

            _commandReadyEvent += CommandReady;
            _outputLineReceivedEvent += OutputLineReceived;
            Console.WriteLine($"[kudu] Connecting to Kudu service at `{argvHost}`...");
            await _connection.Start();
        }

        private static void ConnectionStateChanged(StateChange obj)
        {
            Console.WriteLine("[kudu] DEBUG: State changed to {0}", obj.NewState.ToString());
        }

        private static void CommandError(Exception obj)
        {
            Console.WriteLine("[kudu] Error: {0}", obj.Message);
            _connection.Stop();
        }

        private static void ConnectionClosed()
        {
            Console.WriteLine("[kudu] Connection closed.");
            _closedEvent.Set();
        }

        private static StringBuilder _lastLine = new StringBuilder();

        private static void DataReceived(string data)
        {
            var message = JsonConvert.DeserializeObject<KuduMessage>(data);
            if (message.Output != null && message.Output.EndsWith(">"))
            {
                _ready++;
                _commandReadyEvent?.Invoke(_connection, message);
            }
            else
            {
                foreach (char ch in message.Data)
                {
                    if (ch == '\n')
                    {
                        _outputLineReceivedEvent?.Invoke(_connection, _lastLine.ToString());
                        _lastLine.Clear();
                    }
                    else if (ch != '\r')
                    {
                        _lastLine.Append(ch);
                    }
                }
            }
        }

        private static void CommandReady(object sender, KuduMessage e)
        {
            if (_ready == 1)
            {
                Console.WriteLine($"[kudu] Invoking command `{argvCommand}`...");
                _connection.Send(argvCommand + "\n").Wait();
            }
            else
            {
                _connection.Stop();
            }
        }

        private static void OutputLineReceived(object sender, string e)
        {
            Console.WriteLine($"[kudu] Remote: {e}");
        }

        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: KuduCommandSignalR.exe [host] [username] [password] [command]");
                return;
            }

            argvHost = args[0];
            argvUsername = args[1];
            argvPassword = args[2];
            argvCommand = args[3];

            if (args.Length > 4)
            {
                for (int i = 4; i < args.Length; ++i)
                {
                    switch (args[i])
                    {
                        case "--debug":
                            optDebugTrace = true;
                            break;
                        default:
                            Console.WriteLine("Unknown argv {0}", args[i]);
                            return;
                    }
                }
            }

            SignalRTask().Wait();
            _closedEvent.WaitOne();

            Console.WriteLine("[kudu] Exiting...");
        }
    }
}
