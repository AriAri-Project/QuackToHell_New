using System;

public class User
{
    public string Email { get; set; }
}

public interface IUserRepository
{
    bool Exists(string email);
    void Add(User user);
}

public interface IEmailService
{
    void SendWelcomeEmail(string email);
}

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UserService(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public void RegisterUser(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (_userRepository.Exists(user.Email))
            throw new InvalidOperationException("User already exists.");

        _userRepository.Add(user);

        _emailService.SendWelcomeEmail(user.Email);
    }
}
