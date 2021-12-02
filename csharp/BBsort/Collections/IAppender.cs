namespace Flexols.Data.Collections
{
    public interface IAppender<T>
    {
        void Append(T value);
    }
}