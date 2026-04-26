namespace Worker.Services;

public class WordGenerator
{
    private readonly string _alphabet;
    
    public WordGenerator(string alphabet)
    {
        _alphabet = alphabet;
    }
    
    /// <summary>
    /// Генерирует слово по индексу в комбинаторном пространстве
    /// </summary>
    public string GetWordByIndex(long index)
    {
        if (index < 0) 
            throw new ArgumentException("Index cannot be negative", nameof(index));
        
        // Находим длину слова
        long cumulative = 0;
        int length = 1;
        long countForLength = _alphabet.Length;
        
        while (index >= cumulative + countForLength)
        {
            cumulative += countForLength;
            length++;
            countForLength *= _alphabet.Length;
        }
        
        // Генерируем слово
        long pos = index - cumulative;
        char[] result = new char[length];
        
        for (int i = length - 1; i >= 0; i--)
        {
            result[i] = _alphabet[(int)(pos % _alphabet.Length)];
            pos /= _alphabet.Length;
        }
        
        return new string(result);
    }
}