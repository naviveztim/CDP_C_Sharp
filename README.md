# CDP_C_Sharp
// This is C# implementation of CDP (Concatenated Decisions Path) algorithm 

# Main advantages
- Very fast to train
- Very small model size 
- Easily add new classes 
- High accuracy 

Table 1. Training time and accuracy of **C# implementation** of CDP method

| UCR Dataset  | Num. classes | Num. train samples | Num. test samples | Training time, [sec] | Accuracy, [%] | Compression rate | Num. decision trees | Normalize | Derivative |
|--------------|--------------|--------------------|-------------------|----------------------|---------------|------------------|---------------------|-----------|------------|
| Swedish Leaf | 15           | 500                | 625               | 16.3                 | 92.7%         | 2                | 700                 | No        | No         |
| Beef         | 5            | 30                 | 30                | 24.1                 | 86.8%         | 1                | 400                 | Yes       | Yes        |
| OliveOil     | 4            | 30                 | 30                | 71.3                 | 90.1%         | 2                | 200                 | Yes       | No         |
| Symbols      | 6            | 25                 | 995               | 3.8                  | 95.6%         | 4                | 600                 | Yes       | Yes        |
| OsuLeaf      | 6            | 200                | 242               | 15.1                 | 88.9%         | 4                | 800                 | Yes       | Yes        |
