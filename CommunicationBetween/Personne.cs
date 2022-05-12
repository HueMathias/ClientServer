namespace CommunicationBetween;

public  class Personne
{
    public int id { get; set; }
    public int age { get; set; }
    public string name { get; set; }
    public string ip { get; set; }

    public Personne(int age, string name, string ip)
    {
        this.age = age; 
        this.name = name;
        this.ip = ip;
    }

    public Personne() { }
}
