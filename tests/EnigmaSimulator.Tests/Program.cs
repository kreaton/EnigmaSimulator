using EnigmaSimulator.Core;

int failures = 0;
void Check(string name, string expected, string actual)
{
    bool ok = expected == actual;
    if (!ok) failures++;
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}: expected {expected}, got {actual}");
}

string Run(EnigmaMachine m, string text) =>
    new string(text.Select(ch => EnigmaMachine.Letter(m.Press(ch - 'A').Output)).ToArray());

void Set(EnigmaMachine m, int[] rotors, int refl, string rings, string pos)
{
    for (int i = 0; i < 3; i++) { m.RotorIds[i] = rotors[i]; m.Rings[i] = rings[i] - 'A'; m.Positions[i] = pos[i] - 'A'; }
    m.ReflectorId = refl;
}

// 1. Classic vector: I-II-III, UKW-B, rings AAA, start AAA: AAAAA -> BDZGO
var m1 = new EnigmaMachine(); Set(m1, new[] {0,1,2}, 1, "AAA", "AAA");
Check("AAAAA", "BDZGO", Run(m1, "AAAAA"));

// 2. Long run, same rotors: 'A' x 25 -> known BDZGOWCXLTKSBTMCDLPBMUQOF
var m2 = new EnigmaMachine(); Set(m2, new[] {0,1,2}, 1, "AAA", "AAA");
Check("A x25", "BDZGOWCXLTKSBTMCDLPBMUQOF", Run(m2, new string('A', 25)));

// 3. Double stepping: ADU -> ADV -> AEW -> BFX -> BFY
var m3 = new EnigmaMachine(); Set(m3, new[] {0,1,2}, 1, "AAA", "ADU");
var seq = new List<string> { "ADU" };
for (int i = 0; i < 4; i++) { m3.Step(); seq.Add(string.Concat(m3.Positions.Select(p => EnigmaMachine.Letter(p)))); }
Check("double step", "ADU ADV AEW BFX BFY", string.Join(' ', seq));

// 4. Reciprocity with plugboard, rings, VI/VII/VIII rotors, UKW-C
string plain = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOGZZZ";
var enc = new EnigmaMachine(); Set(enc, new[] {7,5,3}, 2, "CQM", "XYZ");
enc.TryPlug('A'-'A','B'-'A'); enc.TryPlug('C'-'A','D'-'A'); enc.TryPlug('Q'-'A','W'-'A');
string cipher = Run(enc, plain);
var dec = new EnigmaMachine(); Set(dec, new[] {7,5,3}, 2, "CQM", "XYZ");
dec.TryPlug(0,1); dec.TryPlug(2,3); dec.TryPlug('Q'-'A','W'-'A');
Check("reciprocal", plain, Run(dec, cipher));
Console.WriteLine("cipher: " + cipher);
bool neverSelf = plain.Zip(cipher).All(t => t.First != t.Second);
Check("no letter maps to itself", "True", neverSelf.ToString());

// 5. Plugboard rules
var m5 = new EnigmaMachine();
bool a = m5.TryPlug(0,1), b = m5.TryPlug(1,2), c = m5.TryPlug(3,3);
for (int i = 0; i < 4; i++) m5.TryPlug(4 + 2*i, 5 + 2*i);   // 4 more -> total 5
Check("plug rules", "True False False 5", $"{a} {b} {c} {m5.PlugCount}");

Console.WriteLine(failures == 0 ? "ALL OK" : $"{failures} FAILURES");
return failures;
