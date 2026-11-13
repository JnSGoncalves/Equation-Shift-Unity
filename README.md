# Equation Shift

Equation Shift é um jogo de puzzle 2D em que você controla uma interrogação num mundo habitado por sinais. Depois que o Erro foge para as Ruínas da Matemática, empurre números e operadores, assuma novas formas e resolva as equações que mantêm o mundo em equilíbrio.

## O projeto

O jogo foi desenvolvido em **Unity 6000.2.14f1**, em C#, com salas de Tilemap, movimentação em grade e sprites animados em pixel art. As fases usam prefabs para o protagonista, os blocos, os NPCs, os portões e as alavancas. Um validador lê o tabuleiro após os movimentos, calcula os dois lados de cada igualdade e identifica os participantes. As transições conectam cenas vizinhas pelos códigos das salas e mantêm as formas coletadas durante a partida.

Para jogar no Editor, abra `Assets/Scenes/Title.unity`, entre em Play Mode e clique em **Jogar**. Use **WASD** ou **setas** para se mover, **Tab** para alternar entre formas coletadas e **R** para voltar ao ponto inicial da sala e restaurar a posição dos blocos.

- [Manual de instruções em PDF](output/pdf/Manual%20de%20Instruções%20-%20Equation%20Shift.pdf)
- [Rascunho do manual](Manual%20de%20Instruções.md)
- [Guia de fases, percurso e soluções](Guia%20de%20Fases.md)
- [Documentação dos componentes de gameplay](Assets/Scripts/Gameplay/README.md)

## GDD atualizado

### Conceito e experiência

O jogador avança ao transformar uma igualdade escrita em uma solução física. Ele precisa calcular a conta, identificar as peças relevantes e encontrar espaço para empurrá-las. As primeiras salas ensinam o alinhamento e o uso do protagonista como operador. Os desafios posteriores combinam números de dois dígitos, precedência de operações, vários portões e retornos a salas já visitadas.

O ritmo é de observação, tentativa e revisão. O erro de montagem costuma custar novos movimentos. Quando uma peça fica presa, `R` permite refazer o percurso da sala.

### Mundo e personagens

Na **Vila dos Sinais**, interrogações, sinais de igual, mais, vezes e divisão conversam com o jogador quando ele se aproxima. O guardião junto ao portão apresenta as **Ruínas da Matemática**, onde estão as funções que regem o mundo. Após conversar com cinco moradores diferentes, incluindo o Erro, o jogador vê o antagonista abrir o portão e fugir. O tremor que se segue altera as falas da vila.

- **Interrogação (`?`)** é o protagonista. Empurra blocos desde o início e ganha a capacidade de representar outros símbolos ao encontrar os coletáveis. A busca pelas funções acompanha a descoberta de sua própria utilidade.
- **Erro** é o antagonista, identificado pelo sprite vermelho em forma de X. Entediado na vila, mexe nas equações das ruínas e provoca a instabilidade. Ele aparece em G1, G2 e A4.
- **Moradores da vila** dão contexto ao mundo e reagem à fuga e ao tremor por meio de diálogos de proximidade.

### Mecânicas

| Mecânica | Funcionamento na campanha |
|---|---|
| Movimento e blocos | O protagonista se move nas quatro direções. Para empurrar um bloco móvel, ande na direção dele quando a célula seguinte estiver livre. Blocos fixos e âncoras permanecem no lugar. |
| Igualdades | Alinhe números, operadores e exatamente um `=`. A leitura é da esquerda para a direita na horizontal e de cima para baixo na vertical. Dígitos adjacentes na mesma linha compõem números maiores. Multiplicação e divisão precedem soma e subtração. |
| Transformações | Colete Igual (`=`) em H3, Mais (`+`) em D6, Menos (`-`) em G7, Vezes (`*`) em B5 e Divisão (`/`) em D7. O coletável equipa a forma, e `Tab` percorre as formas liberadas. Para participar da equação, pare na célula que precisa do símbolo. |
| Portões de equação | Uma igualdade verdadeira deve incluir a âncora do portão. Treze portões também exigem a participação do protagonista na forma configurada. O portão D de A4 exige as duas âncoras na mesma igualdade. |
| Alavancas | Aproxime-se por uma das quatro células adjacentes. As duas alavancas, em C2 e C6, liberam o portão superior de B4 para A4. |
| Reinício e continuidade | `R` devolve os blocos às suas posições iniciais e o protagonista ao ponto inicial ou de entrada. A troca de sala transporta as formas coletadas. **Jogar** inicia uma nova partida e reinicia o progresso narrativo e das alavancas. |

### Progressão e cenas narrativas

1. **G1, Vila dos Sinais.** O tutorial apresenta o movimento. As conversas revelam o papel das funções e liberam a cena da fuga do Erro.
2. **G2, entrada das ruínas.** Um tremor marca o reencontro com o Erro. Ele sai pelo portão da direita e o fecha, introduzindo o primeiro puzzle de equação.
3. **Ruínas.** A campanha intercala desafios de blocos, salas de obtenção de formas e caminhos de ida e volta. Ao reunir as duas alavancas, o jogador pode avançar de B4 para A4.
4. **A4, sala final.** O Erro admite que mexeu nas equações, empurra o protagonista de lado na fuga e fecha a passagem. Quatro contas menores liberam peças para a igualdade principal. Uma conta correta com apenas uma das âncoras exibe uma dica. Com as duas âncoras na mesma conta verdadeira, o protagonista fala sobre encontrar seu propósito, a tela escurece e aparece **Fim de Jogo**. **Jogar Novamente** retorna ao título.

### Direção visual e sonora

Sprites e elementos de Tilemap em pixel art formam um tabuleiro visto de cima. O título usa Monocraft, fundo escuro, destaque âmbar e blocos animados. Os diálogos usam a mesma fonte sobre fundo preto. O tremor ambiente de 1,3 segundo sinaliza a instabilidade nas fases fora de G1. A vila tem seu próprio tremor narrativo.

Os efeitos sonoros marcam os botões dos menus, o deslocamento de blocos, a coleta e a troca de forma, as equações que liberam portões, a abertura e o fechamento dos portões, as alavancas e os tremores. Os clipes são sintetizados e ficam em `Assets/Resources/Audio`.

O GDD original serviu de referência para a premissa e os personagens. As regras, salas e cenas acima descrevem a versão implementada no projeto.
