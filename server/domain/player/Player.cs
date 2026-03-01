namespace server.domain.player;

class Player(string id, string name)
{
    public string Id { get; } = id;
    public string Name { get; set; } = name;
}