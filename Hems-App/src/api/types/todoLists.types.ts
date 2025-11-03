// Todo Lists Types
export interface LookupDto {
  id: number;
  title?: string;
}

export interface TodoListDto {
  id: number;
  title?: string;
  colour?: string;
  items: {
    id: number;
    listId: number;
    title?: string;
    done: boolean;
    priority: number;
    note?: string;
  }[];
}

export interface CreateTodoListCommand {
  title?: string;
}

export interface UpdateTodoListCommand {
  id: number;
  title?: string;
}

export interface TodosVm {
  priorityLevels: LookupDto[];
  lists: TodoListDto[];
}


export interface CreateTodoListParams {
  id: number;
}

export interface UpdateTodoListParams {
  id: number;
}

export interface DeleteTodoListParams {
  id: number;
}
