using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RabbitConsumerCore
{
    class Program
    {
        private static IConnection m_connection;
        private static IModel m_channel;
        private static ConnectionFactory m_factory;
        private static void Enqueue(string queue_name, string json)
        {
            // push message into rabbit
            try
            {
                Console.WriteLine("Sending ... ", json);
                m_channel.QueueDeclare(queue: queue_name,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(json);
                m_channel.BasicPublish(exchange: "",
                                     routingKey: queue_name,
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine(" [x] Sent {0}", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"not ready ... {ex}");
            }
        }

        static void HandleRequest(object model, BasicDeliverEventArgs e)
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            //Console.WriteLine(" [x] Received {0}", message);

            Task.Run(() =>
            {
                Console.WriteLine(" Worker executing " + message);
                Message msg = JsonConvert.DeserializeObject<Message>(message);
                Console.WriteLine("fire stored procedure...");
                
                msg.db_rows = "db rows...";

                //string json = $"{{ corr_id: '{msg.corr_id}', sp_name: '{msg.sp_name}', result: 'db rows' }}";

                string json = JsonConvert.SerializeObject(msg);
                Enqueue(msg.queue_result_name, json);
            });
        }
        static void Main(string[] args)
        {
            m_factory = new ConnectionFactory() { HostName = "10.0.0.28" };
            //factory.Port = 15699;
            //var factory = new ConnectionFactory() { HostName = "localhost" };
            m_factory.Port = 5677;
            //factory.Port = 5677;
            m_connection = m_factory.CreateConnection();
            m_channel = m_connection.CreateModel();

            Console.WriteLine("Rabbit for db-gateway is ready ...");

            m_channel.QueueDeclare(queue: "mq_read_outbox",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

            var consumer = new EventingBasicConsumer(m_channel);

            consumer.Received += HandleRequest;

            m_channel.BasicConsume(queue: "mq_read_outbox",
                                         autoAck: true,
                                         consumer: consumer);
            // Before Dispose!!!
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();

        }
    }
}
