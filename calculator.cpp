#include <iostream>
#include <limits>

int main() {
    std::cout << "Simple Calculator (supports + - * /).\n";

    double left = 0.0;
    double right = 0.0;
    char op = 0;

    std::cout << "Enter expression (e.g., 3.5 * 2): ";
    if (!(std::cin >> left >> op >> right)) {
        std::cerr << "Invalid input.\n";
        return 1;
    }

    double result = 0.0;
    switch (op) {
        case '+':
            result = left + right;
            break;
        case '-':
            result = left - right;
            break;
        case '*':
            result = left * right;
            break;
        case '/':
            if (right == 0.0) {
                std::cerr << "Error: division by zero.\n";
                return 1;
            }
            result = left / right;
            break;
        default:
            std::cerr << "Unsupported operator.\n";
            return 1;
    }

    std::cout << "Result: " << result << "\n";
    return 0;
}
