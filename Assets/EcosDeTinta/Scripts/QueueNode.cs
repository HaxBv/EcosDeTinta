using UnityEngine;

public class QueueNode<T>
{
    #region Properties/Privates
    private T value = default;
    private QueueNode<T> next = null;
    #endregion


    #region Methods
    public QueueNode(T value)
    {
        this.value = value;
        this.next = null;
    }

    public void SetNext(QueueNode<T> next)
    {
        this.next = next;
    }
    #endregion

    #region Getters

    public QueueNode<T> Next => next;
    public T Value => value;
    #endregion


}
