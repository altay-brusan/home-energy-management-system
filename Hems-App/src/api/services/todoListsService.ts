import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  TodosVm,
  CreateTodoListCommand,
  UpdateTodoListCommand,
  CreateTodoListParams,
  UpdateTodoListParams,
  DeleteTodoListParams,
} from '../types/todoLists.types';

export const todoListsService = {
  // Get todo lists
  getTodoLists: async (): Promise<TodosVm> => {
    const response = await apiClient.get<TodosVm>(ENDPOINTS.TODO_LISTS.GET_ALL);
    return response.data;
  },

  // Create todo list
  createTodoList: async (
    command: CreateTodoListCommand
  ): Promise<number> => {
    const response = await apiClient.post<number>(ENDPOINTS.TODO_LISTS.CREATE, command);
    return response.data;
  },

  // Update todo list
  updateTodoList: async (
    params: UpdateTodoListParams,
    command: UpdateTodoListCommand
  ): Promise<void> => {
    const url = ENDPOINTS.TODO_LISTS.UPDATE.replace(':id', params.id.toString());
    await apiClient.put<void>(url, command);
  },

  // Delete todo list
  deleteTodoList: async (params: DeleteTodoListParams): Promise<void> => {
    const url = ENDPOINTS.TODO_LISTS.DELETE.replace(':id', params.id.toString());
    await apiClient.delete<void>(url);
  },
};
