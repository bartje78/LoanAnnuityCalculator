# Loan Annuity Calculator

This project is a simple console application that calculates the monthly annuity on a loan based on user-provided input parameters such as interest rate, tenor, and loan amount. It also breaks down the annuity into its interest and capital components.

## Project Structure

- **LoanAnnuityCalculator.sln**: Solution file that organizes the project and its components.
- **Program.cs**: Entry point of the application. Prompts the user for input and calls the `AnnuityCalculator` service.
- **Models/Loan.cs**: Defines the `Loan` class with properties like `Amount`, `InterestRate`, and `Tenor`.
- **Services/AnnuityCalculator.cs**: Contains methods to calculate the monthly annuity, interest component, and capital component.
- **Utils/InputValidator.cs**: Provides methods to validate user input for interest rate and tenor.

## How to Run the Application

1. Clone the repository or download the project files.
2. Open the solution file `LoanAnnuityCalculator.sln` in your preferred IDE.
3. Build the solution to restore any dependencies.
4. Run the application. You will be prompted to enter the loan amount, interest rate, and tenor.
5. The application will display the monthly annuity, interest component, and capital component.

## Input Parameters

- **Loan Amount**: The total amount of the loan.
- **Interest Rate**: The annual interest rate (in percentage).
- **Tenor**: The duration of the loan in months.

## Example Usage

```
Enter Loan Amount: 10000
Enter Interest Rate (annual %): 5
Enter Tenor (months): 24

Monthly Annuity: $438.71
Interest Component: $250.00
Capital Component: $188.71
```

## License

This project is licensed under the MIT License. See the LICENSE file for more details.