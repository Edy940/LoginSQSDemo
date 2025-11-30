import { useState } from "react";

const API_BASE = "http://localhost:5235"; // porta da sua Auth.Api

function App() {
  const [tab, setTab] = useState("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const [token, setToken] = useState(null);

  async function handleRegister(e) {
    e.preventDefault();
    setMessage("");

    const res = await fetch(`${API_BASE}/api/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });

    if (res.ok) {
      setMessage("Usuário cadastrado com sucesso! 🎉");
    } else {
      const text = await res.text();
      setMessage("Erro no cadastro: " + text);
    }
  }

  async function handleLogin(e) {
    e.preventDefault();
    setMessage("");

    const res = await fetch(`${API_BASE}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });

    if (res.ok) {
      const data = await res.json();
      setToken(data.token);
      setMessage("Login realizado!");
    } else {
      setMessage("Credenciais inválidas.");
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: "40px auto", fontFamily: "sans-serif" }}>
      <h1>LoginSqsDemo</h1>

      <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
        <button onClick={() => setTab("login")} disabled={tab === "login"}>
          Login
        </button>
        <button onClick={() => setTab("register")} disabled={tab === "register"}>
          Cadastro
        </button>
      </div>

      {tab === "register" && (
        <form onSubmit={handleRegister}>
          <input
            type="email"
            placeholder="E-mail"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
          />
          <br />
          <input
            type="password"
            placeholder="Senha"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
          />
          <br />
          <button type="submit">Cadastrar</button>
        </form>
      )}

      {tab === "login" && (
        <form onSubmit={handleLogin}>
          <input
            type="email"
            placeholder="E-mail"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
          />
          <br />
          <input
            type="password"
            placeholder="Senha"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
          />
          <br />
          <button type="submit">Entrar</button>
        </form>
      )}

      {message && <p style={{ marginTop: 16 }}>{message}</p>}

      {token && (
        <div style={{ marginTop: 16 }}>
          <strong>Token (fake):</strong>
          <pre>{token}</pre>
        </div>
      )}
    </div>
  );
}

export default App;
