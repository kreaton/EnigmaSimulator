namespace EnigmaSimulator.Core;

/// <summary>Wiring and turnover notch(es) of one historical rotor (Walze).</summary>
public sealed record RotorSpec(string Name, string Wiring, string Notches);

/// <summary>Result of enciphering one letter, including every stage of the electrical path.</summary>
public sealed record EncipherResult(int Input, int Output, int[] Path)
{
    /// <summary>Human-readable labels for the 10 entries of <see cref="Path"/>.</summary>
    public static readonly string[] StageNames =
    {
        "Tastatur", "Steckerbrett", "Walze R", "Walze M", "Walze L",
        "Umkehrwalze", "Walze L", "Walze M", "Walze R", "Steckerbrett → Lampe"
    };
}

/// <summary>
/// Wehrmacht Enigma I / M3 (three rotors out of I–VIII, reflectors A/B/C, ring settings, plugboard)
/// including the real double-stepping anomaly of the middle rotor.
/// Letters are 0..25 (A..Z).
/// </summary>
public sealed class EnigmaMachine
{
    public static readonly RotorSpec[] Rotors =
    {
        new("I",    "EKMFLGDQVZNTOWYHXUSPAIBRCJ", "Q"),
        new("II",   "AJDKSIRUXBLHWTMCQGZNPYFVOE", "E"),
        new("III",  "BDFHJLCPRTXVZNYEIWGAKMUSQO", "V"),
        new("IV",   "ESOVPZJAYQUIRHXLNFTGKDCMWB", "J"),
        new("V",    "VZBRGITYUPSDNHLXAWMJQOFECK", "Z"),
        new("VI",   "JPGVOUMFYQBENHZRDKASXLICTW", "ZM"),
        new("VII",  "NZJHGRCXMYSWBOUFAIVLPEKQDT", "ZM"),
        new("VIII", "FKQHTLXOCBJSPDZRAMEWNIUYGV", "ZM"),
    };

    public static readonly RotorSpec[] Reflectors =
    {
        new("UKW-A", "EJMZALYXVBWFCRQUONTSPIKHGD", ""),
        new("UKW-B", "YRUHQSLDPXNGOKMIEBFZCWVJAT", ""),
        new("UKW-C", "FVPJIAOYEDRZXWGCTKUQSBNMHL", ""),
    };

    // Index 0 = left, 1 = middle, 2 = right rotor (as seen by the operator).
    public int[] RotorIds { get; } = { 0, 1, 2 };
    public int[] Rings { get; } = { 0, 0, 0 };      // Ringstellung 0..25
    public int[] Positions { get; } = { 0, 0, 0 };  // visible window letter 0..25
    public int ReflectorId { get; set; } = 1;       // UKW-B

    private readonly int[] _plug = new int[26];

    public EnigmaMachine() => ClearPlugboard();

    // ---------------- plugboard ----------------

    public int PlugPartner(int c) => _plug[c];

    public bool IsPlugged(int c) => _plug[c] != c;

    public int PlugCount => _plug.Where((p, i) => p > i).Count();

    public IEnumerable<(int A, int B)> PlugPairs() =>
        Enumerable.Range(0, 26).Where(i => _plug[i] > i).Select(i => (i, _plug[i]));

    public void ClearPlugboard()
    {
        for (int i = 0; i < 26; i++) _plug[i] = i;
    }

    /// <summary>Connect two free letters with a cable. Max. 10 cables like the real Steckerbrett.</summary>
    public bool TryPlug(int a, int b)
    {
        if (a == b || IsPlugged(a) || IsPlugged(b) || PlugCount >= 10) return false;
        _plug[a] = b;
        _plug[b] = a;
        return true;
    }

    public void Unplug(int c)
    {
        int p = _plug[c];
        _plug[c] = c;
        _plug[p] = p;
    }

    // ---------------- stepping ----------------

    private bool AtNotch(int slot) =>
        Rotors[RotorIds[slot]].Notches.Contains((char)('A' + Positions[slot]));

    /// <summary>Advance the rotors (before every letter), including double-stepping.</summary>
    public void Step()
    {
        bool middleAtNotch = AtNotch(1);
        bool rightAtNotch = AtNotch(2);

        if (middleAtNotch)
        {
            // Middle rotor pushes the left rotor and (double step) also moves itself.
            Positions[0] = (Positions[0] + 1) % 26;
            Positions[1] = (Positions[1] + 1) % 26;
        }
        else if (rightAtNotch)
        {
            Positions[1] = (Positions[1] + 1) % 26;
        }
        Positions[2] = (Positions[2] + 1) % 26;
    }

    // ---------------- enciphering ----------------

    private int Forward(RotorSpec spec, int slot, int c)
    {
        int shift = Positions[slot] - Rings[slot];
        int entry = Mod(c + shift);
        int wired = spec.Wiring[entry] - 'A';
        return Mod(wired - shift);
    }

    private int Backward(RotorSpec spec, int slot, int c)
    {
        int shift = Positions[slot] - Rings[slot];
        int entry = Mod(c + shift);
        int wired = spec.Wiring.IndexOf((char)('A' + entry));
        return Mod(wired - shift);
    }

    private static int Mod(int x) => ((x % 26) + 26) % 26;

    /// <summary>Press a key: steps the rotors, then runs the signal through the machine.</summary>
    public EncipherResult Press(int key)
    {
        Step();
        return Encipher(key);
    }

    /// <summary>Run the signal through the machine without stepping (current rotor positions).</summary>
    public EncipherResult Encipher(int key)
    {
        var path = new int[10];
        int c = key;
        path[0] = c;
        c = _plug[c]; path[1] = c;

        for (int slot = 2; slot >= 0; slot--)
        {
            c = Forward(Rotors[RotorIds[slot]], slot, c);
            path[2 + (2 - slot)] = c;
        }

        c = Reflectors[ReflectorId].Wiring[c] - 'A';
        path[5] = c;

        for (int slot = 0; slot <= 2; slot++)
        {
            c = Backward(Rotors[RotorIds[slot]], slot, c);
            path[6 + slot] = c;
        }

        c = _plug[c]; path[9] = c;
        return new EncipherResult(key, c, path);
    }

    // ---------------- helpers for the UI ----------------

    /// <summary>True if the rotor in this slot currently shows its turnover notch.</summary>
    public bool NotchVisible(int slot) => AtNotch(slot);

    public static char Letter(int c) => (char)('A' + Mod(c));
}
