Console.WriteLine("Hello, World!");

var (a, b) = G();

Console.WriteLine(b);

(int, string) G()
{
    return (1, "hi");
}