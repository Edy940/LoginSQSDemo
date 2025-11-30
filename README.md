\# LoginSqsDemo — API .NET + React + Microsserviços com AWS SQS



Projeto desenvolvido com foco em demonstrar domínio de \*\*C#/.NET\*\*, \*\*React\*\*, \*\*AWS (SQS)\*\*, 

\*\*mensageria\*\*, \*\*microsserviços\*\*, boas práticas de \*\*Clean Code\*\* e organização 

de fluxo Git no padrão \*\*GitFlow\*\* (main/develop/feature/hotfix).



---



\## 🚀 Visão Geral



O projeto consiste em um pequeno sistema com:



\- \*\*Frontend React\*\* com telas de \*\*Login\*\* e \*\*Cadastro\*\*

\- \*\*API em .NET 8+ (Auth.Api)\*\* para autenticação

\- \*\*Worker em .NET (Auth.Worker)\*\* consumindo mensagens da fila SQS

\- \*\*Mensageria AWS SQS\*\* para comunicação assíncrona entre serviços

\- Arquitetura baseada em \*\*microserviços\*\* (API → eventos → worker)



Fluxo básico:



1\. O usuário faz o \*\*cadastro\*\* pelo React ou Swagger.

2\. A API registra o usuário e publica um \*\*evento em JSON no SQS\*\*.

3\. O \*\*Worker\*\* consome a mensagem da fila e processa o evento.

4\. O sistema se mantém escalável, desacoplado e pronto para ambiente cloud.



---



\## 🧱 Arquitetura



\- \*\*Auth.Api\*\* expõe endpoints REST:

&nbsp; - `POST /api/auth/register`

&nbsp; - `POST /api/auth/login`



\- \*\*Auth.Worker\*\*:

&nbsp; - Lê mensagens da fila SQS

&nbsp; - Processa eventos de usuário registrado

&nbsp; - Deleta mensagens após o processamento

&nbsp; - Escreve logs estruturados



---



\## 🛠️ Tecnologias Utilizadas



\### Backend

\- .NET 8+ (Minimal APIs)

\- BCrypt.Net (hash de senha)

\- AWS SDK for .NET (SQS)

\- Injeção de dependência (DI)

\- Clean Code / boas práticas



\### Frontend

\- React + Vite

\- Fetch API

\- Componentes simples e funcionais



\### Cloud / Arquitetura

\- AWS SQS (mensageria)

\- Comunicação assíncrona orientada a eventos

\- Microsserviços independentes (API \& Worker)



\### Git / Organização

\- GitFlow:

&nbsp; - `main`

&nbsp; - `develop`

&nbsp; - `feature/\*`

&nbsp; - `hotfix/\*`

\- Commits semânticos (feat, fix, chore…)



---



\## 🗂️ Estrutura de Pastas



```text

LoginSqsDemo/

├── backend/

│   ├── Auth.Api/

│   └── Auth.Worker/

├── frontend/

│   └── login-sqs-react/

├── appsettings.example.json

└── README.md



