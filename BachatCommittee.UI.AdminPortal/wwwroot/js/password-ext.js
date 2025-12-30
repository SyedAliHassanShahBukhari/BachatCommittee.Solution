
function generatePassword(length = 15) {
    const minLength = 8;
    const maxLength = 100;

    // Enforce boundaries
    if (length < minLength) length = minLength;
    if (length > maxLength) length = maxLength;

    const lower = "abcdefghijklmnopqrstuvwxyz";
    const upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const numbers = "0123456789";
    const symbols = "!@@^*()-_=+[]{};:,";
    const allChars = lower + upper + numbers + symbols;

    let password = "";
    const array = new Uint32Array(length);
    crypto.getRandomValues(array);

    for (let i = 0; i < length; i++) {
        password += allChars[array[i] % allChars.length];
    }

    // Ensure at least one of each type (lower, upper, number, symbol)
    if (!/[a-z]/.test(password)) password += lower[Math.floor(Math.random() * lower.length)];
    if (!/[A-Z]/.test(password)) password += upper[Math.floor(Math.random() * upper.length)];
    if (!/[0-9]/.test(password)) password += numbers[Math.floor(Math.random() * numbers.length)];
    if (!/[!@@^*()-_=+[]{};:,]/.test(password))
        password += symbols[Math.floor(Math.random() * symbols.length)];

    return password
        .split("")
        .sort(() => Math.random() - 0.5)
        .join("")
        .slice(0, length);
}
