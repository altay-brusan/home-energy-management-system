import { PriorityLevel } from "./common.types";

export interface TodoItemBriefDto {
  id: number;
  listId: number;
  title?: string;
  done: boolean;
}

export interface TodoItemDto {
  id: number;
  listId: number;
  title?: string;
  done: boolean;
  priority: number;
  note?: string;
}

export interface CreateTodoItemCommand {
  listId: number;
  title?: string;
}

export interface UpdateTodoItemCommand {
  id: number;
  title?: string;
  done: boolean;
}

export interface UpdateTodoItemDetailCommand {
  id: number;
  listId: number;
  priority: PriorityLevel;
  note?: string;
}

export interface PaginatedListOfTodoItemBriefDto {
  items: TodoItemBriefDto[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GetTodoItemsWithPaginationParams {
  ListId: number;
  PageNumber: number;
  PageSize: number;
}

export interface UpdateTodoItemParams {
  id: number;
}

export interface UpdateTodoItemDetailParams {
  id: number;
}

export interface DeleteTodoItemParams {
  id: number;
}
