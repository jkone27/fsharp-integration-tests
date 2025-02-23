// https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient

// DTO
public record Person(string Name, int Age);


// http typed client
public class PersonClient 
{
    private HttpClient httpClient;
    private readonly Random rnd = new Random();

    public PersonClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<Person?> GetByName(string name) 
    {
        var result = await httpClient.GetFromJsonAsync<Person[]>($"persons?name={name}");

        return result?.FirstOrDefault();
    }
    
}

// service using this client
public class PersonRepository 
{
    private readonly PersonClient personClient;
    public PersonRepository(PersonClient personClient)
    {
        this.personClient = personClient;
    }

    public Task<Person?> GetPerson(string name) => personClient.GetByName(name);
}