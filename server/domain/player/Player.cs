namespace server.domain.player;

public class Player(PlayerId id, string name)
{
    public PlayerId Id { get; } = id;
    public string Name { get; set; } = name;
}
