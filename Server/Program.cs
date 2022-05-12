using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Server;

public class Server
{
    // Incoming data from the client.  
    public static string data = null;

    public static void StartListening()
    {

        
        // Data buffer for incoming data.  
        byte[] bytes = new Byte[1024];

        // Establish the local endpoint for the socket.  
        // Dns.GetHostName returns the name of the
        // host running the application.  
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and
        // listen for incoming connections.  
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start listening for connections.  
            while (true)
            {
                Console.WriteLine("Waiting for a connection...");
                // Program is suspended while waiting for an incoming connection.  
                Socket handler = listener.Accept();
                data = null;

                // An incoming connection needs to be processed.  
                while (true)
                {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.Length > 1)
                    {
                        break;
                    }
                }

                // Show the data on the console.  
                Console.WriteLine("Text received : {0}", data);

                // Echo the data back to the client.  
                byte[] msg = Encoding.ASCII.GetBytes(data);

                handler.Send(msg);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue...");
        Console.Read();

    }

    private static IPAddress ip = new IPAddress(new byte[] { 127, 0, 0, 1 });
    private static Socket listener;

    public static Dictionary<int, CommunicationBetween.Personne> people = new();
    public static void Start()
    {
        IPEndPoint endPoint = new IPEndPoint(ip, 1234);
        listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(endPoint);
        listener.Listen(6);

        Console.WriteLine("Server start");
        LoadJsonFile();
        while (true)
        {
            Socket client = listener.Accept();
            Console.WriteLine("Client connected");
            
            var thread = new Thread(() =>
            {
                SendOK(client);

                bool exist = false;
                CommunicationBetween.Personne theClient = new();
                //Regarde dans fichier json si client existe déjà
                //Récupère ip 
                string ip = Get(client);

                foreach (var personne in people)
                {
                    if (personne.Value.ip.ToString() == ip)
                    {
                        exist = true;
                        theClient = personne.Value;
                    }
                        
                }

                //si oui => envoi ses infos
                if (exist)
                {
                    SendClient(client, theClient);                    
                }
                else
                {
                    //sinon => demande ses infos et on enregistre dans dictionnaire
                    SendKO(client);
                }

                string connect = Get(client);
                if (connect != "OK")
                {
                    string json = JsonConvert.SerializeObject(people);
                    CommunicationBetween.Personne newClient = JsonConvert.DeserializeObject<CommunicationBetween.Personne>(connect);

                    Random aleatoire = new Random();
                    int entier = aleatoire.Next(1, 9999);

                    int id = entier * 1024;
                    newClient.id = id;
                    people.Add(newClient.id, newClient);

                    SaveDictionary(JsonConvert.SerializeObject(people));
                    Console.WriteLine("Client sauvegardé");
                    SendOK(client);
                }
            });
            thread.Start();
        }
    }
    
    public static void SendOK(Socket client)
    {
        byte[] msg = Encoding.ASCII.GetBytes("OK");
        client.Send(msg);
    }

    public static void SendKO(Socket client)
    {
        byte[] msg = Encoding.ASCII.GetBytes("KO");
        client.Send(msg);
    }

    public static void SendClient(Socket client, CommunicationBetween.Personne personne)
    {
        byte[] msg = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(personne));
        client.Send(msg);
    }

    public static void ReceptionPersonne(Socket socket)
    {
        byte[] buffer = new byte[1024];
        int length = socket.Receive(buffer);
        string resp = Encoding.ASCII.GetString(buffer, 0, length);

        CommunicationBetween.Personne? clientConnect = JsonConvert.DeserializeObject<CommunicationBetween.Personne>(resp);
        if (clientConnect != null)
        {
            Random aleatoire = new Random();
            int entier = aleatoire.Next(1, 9999);

            int id = entier * 1024;
            clientConnect.id = id;
            people.Add(id, clientConnect);
            Console.WriteLine("Le client : " + clientConnect.name + " a envoyé ses informations avec succès");
        }
        else
        {
            Console.WriteLine("Le client n'a pas envoyé ses informations");
        }
    }

    public static string Get(Socket socket)
    {
        byte[] buffer = new byte[1024];
        int length = socket.Receive(buffer);
        string resp = Encoding.ASCII.GetString(buffer, 0, length);
        return resp;
    }

    public static void SaveDictionary(string json)
    {
        string path = @"Ressources\people.json";
        path = Path.GetFullPath(path).Replace(@"\bin\Debug\net6.0", "");        

        using StreamWriter file = File.CreateText(path);
        
        JsonSerializer serializer = new();
        serializer.Serialize(file, json);
        
    }

    public static void LoadJsonFile()
    {
        string path = @"Ressources\people.json";
        path = Path.GetFullPath(path).Replace(@"\bin\Debug\net6.0", "");

        string jsonFile = File.ReadAllText(path);
        string jsonConvert = JsonConvert.DeserializeObject<string>(jsonFile);
        if (jsonFile != null && jsonFile != "")
            people = JsonConvert.DeserializeObject<Dictionary<int, CommunicationBetween.Personne>>(jsonConvert);
    }
    
    public static int Main(String[] args)
    {
        Start();
        return 0;
    }
}
