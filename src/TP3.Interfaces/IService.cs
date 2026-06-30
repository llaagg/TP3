public interface IService
{
    object State { get; }
    object Control { get; }
    object Events { get; }

    
    MetaData MetaData { get; }
}