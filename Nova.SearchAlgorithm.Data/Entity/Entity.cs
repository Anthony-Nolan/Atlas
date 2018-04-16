namespace Nova.SearchAlgorithm.Data.Entity
{
    // Setting this up to eventually work with Max's EntityFramework extensions when they have been packed up for reuse.
    public interface IEntity<T>
    {
        T Id { get; set; }
    }

    public class Entity<T> : IEntity<T>
    {
        public T Id { get; set; }
    }
}