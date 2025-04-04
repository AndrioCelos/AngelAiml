﻿using System.Collections;

namespace AngelAiml;
/// <summary>
///		Represents a stack-like structure with a fixed capacity, whose items can also be accessed by index,
///		starting from the most recently added item.
///		If the capacity is reached, adding a new element will drop the oldest item.
/// </summary>
/// <typeparam name="T">The type of the items in the list.</typeparam>
public class History<T> : IReadOnlyCollection<T> {
	private readonly T[] items;
	private int head;

	/// <summary>Returns the number of items in this <see cref="History{T}"/>.</summary>
	public int Count { get; private set; }
	/// <summary>Returns the maximum number of items that can be in this <see cref="History{T}"/>.</summary>
	public int Capacity => items.Length;

	/// <summary>Creates a new <see cref="History{T}"/> object with the specified capacity.</summary>
	/// <exception cref="ArgumentOutOfRangeException">capacity is less than 1.</exception>
	public History(int capacity) {
		if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be at least 1.");
		items = new T[capacity];
	}

	/// <summary>Adds an item to the <see cref="History{T}"/>, dropping the oldest item if the list is full.</summary>
	public void Add(T item) {
		var next = (head + 1) % Capacity;
		items[next] = item;
		head = next;
		if (Count < Capacity) Count++;
	}

	/// <summary>Returns the item with the specified index, starting from the most recently added item at index 0.</summary>
	/// <exception cref="ArgumentOutOfRangeException">index is less than 0 or greater or equal to <see cref="Count"/>.</exception>
	public T this[int index] {
		get {
			if (index < 0 || index >= Count) throw new IndexOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the collection.");
			var position = head - index;
			if (position < 0) position += Capacity;
			return items[position];
		}
	}

	public IEnumerator<T> GetEnumerator() {
		for (var i = 0; i < Count; i++) yield return this[i];
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
