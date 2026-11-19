class Player
{
    private string name;

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public Player(string name)
    {
        this.name = name;
    }

    public virtual bool Eligibility()
    {
        return false;
    }

    public virtual void ShowInfo()
    {
        Console.WriteLine(Name);
        Console.WriteLine(Eligibility() ? "Eligible" : "Not Eligible");
    }
}

class Batsman : Player
{
    private double average;

    public double Average
    {
        get { return average; }
        set { average = value; }
    }

    public Batsman(string name, double average) : base(name)
    {
        this.average = average;
    }

    public override bool Eligibility()
    {
        return Average > 50;
    }
}
